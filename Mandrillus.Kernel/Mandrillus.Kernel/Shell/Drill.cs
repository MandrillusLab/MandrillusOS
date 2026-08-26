// Copyright © 2026 Leandro Vieira / Mandrillus Systems
// Licensed under the MIT License. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using Mosa.DeviceSystem.HardwareAbstraction;
using Mosa.DeviceSystem.Keyboard;

namespace Mandrillus.Kernel.Shell;

/// <summary>
/// Drill - Mandrillus OS's phase 1 interactive shell.
/// A simple native command-loop REPL (MS-DOS / Cosmos-shell style), not a scripting host.
/// Advanced features (pipes, object-like output, scripting, etc.) are deferred to a later phase,
/// once Mandrillus OS has a filesystem and more infrastructure.
/// </summary>
public static class Drill
{
    private const string Prompt = "Drill> ";
    private const int InputBufferCapacity = 256;
    internal const int HistoryCapacity = 32;

    // Mosa.Korlib (the corlib actually pulled in via Mosa.Runtime on the
    // BareMetal target) does not implement Dictionary<TKey,TValue> — only
    // List<T>, Queue<T>, Stack<T>, LinkedList<T>, and the IDictionary
    // interface without a concrete implementation. Dictionary<TKey,TValue>
    // exists only in Mosa.TinyCoreLib, an alternative corlib this project
    // does not reference. Command lookup is therefore done with two
    // parallel lists instead of a hash map — fine at Phase 1's scale
    // (a handful of builtins); revisit if the command count grows enough
    // to justify a real hash table later.
    private static readonly List<string> CommandNames = new List<string>();
    private static readonly List<DrillCommand> CommandList = new List<DrillCommand>();
    private static readonly string[] History = new string[HistoryCapacity];

    private static char[] _inputBuffer = new char[InputBufferCapacity];
    private static int _inputLenght;
    private static bool _running;

    // History is a fixed-size ring buffer (no dynamic growth on the hot path,
    // consistent with _inputBuffer). _historyCount tracks how many entries are
    // populated so far (up to HistoryCapacity); _historyNext is where the next
    // entry will be written. _historyCursor is -1 when not browsing history
    // (i.e. editing a fresh line), or an index into History while Up/Down is
    // being used to navigate past entries.
    private static int _historyCount;
    private static int _historyNext;
    private static int _historyCursor = -1;

    /// <summary>
    /// Registers the built-in commands and starts the shell loop.
    /// Call this once from Program.cs's EntryPoint(), after Boot.cs has ended off
    /// control and kernel/console/keyboard setup is complete - not from Boot.cs
    /// itself, which stays a thin platform-specific entry point (see Mosa.Starter.x86
    /// vs. Mosa.Starter's Boot.cs/Program.cs split).
    /// </summary>
    public static void Start()
    {
        RegisterBuiltins();

        _running = true;
        _inputLenght = 0;

        Console.WriteLine();
        Console.WriteLine("Mandrillus OS - Drill shell");
        Console.WriteLine("Type 'help' for a list of commands.");
        Console.WriteLine();
        WritePrompt();

        while (_running)
        {
            PumpOnce();

            // HAL (Mosa.DeviceSystem) and Kernel.Keyboard (Mosa.Kernel.BareMetal)
            // are both platform-agnostic assemblies - this file has no x86-specific
            // compile-time dependency, which is why Drill shell lives in Mandrillus.Kernel
            // rather than Mandrillus.Kernel.x86. The BEHAVIOR of HAL.Yeld() is what
            // varies per platform, not the code calling it: on x86 it resolves to
            // Native.Hlt() (confirmed via Mosa.Kernel.BareMetal.x86/Plug.cs),
            // safe regardless of Scheduler.Enabled - with the Scheduler inactive, HLT
            // simply wakes on the next interrupt and the loop re-checks; with it
            // active, this is what lets other threads actually run between
            // keystrokes. On x64/ARM32/ARM64 the equivalent plug is commented out in
            // those platforms' PlatformPlug.cs, so HAL.Yeld() falls back to a
            // busy-wait no-op there - same source, degraded runtime behavior, not a
            // compile error. Revisit if Drill ever needs guaranteed yielding on
            // those platforms.
            HAL.Yield();
        }
    }

    /// <summary>
    /// Registers a new command, or replaces an existing one with the same name.
    /// </summary>
    public static void RegisterCommand(string name, string description, DrillCommandHandler handler)
    {
        if (string.IsNullOrEmpty(name))
            return;

        var command = new DrillCommand(name, description, handler);
        var existingIndex = IndexOfCommand(name);

        if (existingIndex >= 0)
        {
            CommandList[existingIndex] = command;
        }
        else
        {
            CommandNames.Add(name);
            CommandList.Add(command);
        }
    }

    private static int IndexOfCommand(string name)
    {
        for (var i = 0; i < CommandNames.Count; i++)
        {
            if (CommandNames[i] == name)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Requests the shell loop to stop after the current iteration.
    /// </summary>
    public static void RequestExit()
    {
        _running = false;
    }

    // ---------------------------------------------------------------
    // Note on Kernel.Keyboard
    // ---------------------------------------------------------------
    // Mosa.Kernel.BareMetal.Kernel.Keyboard is typed as the concrete 'Keyboard'
    // class (not the IKeyboard interface), and is explicitly commented
    // '// temporary hack' in MOSA's own source (Kernel.cs). Residual risk:
    // re-verify this property still exists with this name/type on any future
    // MOSA version bump - same category of risk already tracked for Issue #9's
    // IDT.cs dependency.

    // ---------------------------------------------------------------
    // Main loop internals
    // ---------------------------------------------------------------

    private static void PumpOnce()
    {
        // Non-blocking poll - GetKeyPressed() default to blocking: false.
        // Confirmed safe pattern regardless of Scheduler.Enabled state or platform,
        // per Issue #8 keyboard-read investigation. Key is a class (nullable);
        // returns null both when no scancode is pending and when the scancode
        // was a modifier key (Shift/Ctrl/Alt/CapsLock/etc.) - confirmed directly
        // against Mosa.DeviceSystem.Keyboard.Keyboard.GetKeyPressed() source.
        var key = Mosa.Kernel.BareMetal.Kernel.Keyboard.GetKeyPressed();

        if (key == null)
            return;

        HandleKey(key);
    }

    private static void HandleKey(Key key)
    {
        // Check KeyType first: arrow keys carry no Character (it's 0x00 for
        // non-regular keys per Mosa.DeviceSystem.Keyboard.Keyboard.GetKeyPressed),
        // so they must be matched on KeyType, not Character.
        switch (key.KeyType)
        {
            case KeyType.UpArrow:
                NavigateHistory(-1);
                return;
            case KeyType.DownArrow:
                NavigateHistory(1);
                return;
        }

        switch (key.Character)
        {
            case '\n':
            case '\r':
                Console.WriteLine();
                SubmitLine();
                break;
            case '\b':
                RemoveLastChar();
                break;
            default:
                AppendChar(key.Character);
                break;
        }
    }

    private static void SubmitLine()
    {
        var line = new string(_inputBuffer, 0, _inputLenght);
        _inputLenght = 0;
        _historyCursor = -1;

        if (line.Length > 0)
            PushHistory(line);

        Dispatch(line);

        if (_running)
            WritePrompt();
    }

    // ---------------------------------------------------------------
    // Command history
    // ---------------------------------------------------------------

    private static void PushHistory(string line)
    {
        // Skip exact repeats of the immediately preceding entry, matching
        // common shell behavior (avoids "up" landing on duplicate no-ops).
        if (_historyCount > 0)
        {
            var lastIndex = (_historyNext - 1 + HistoryCapacity) % HistoryCapacity;
            if (History[lastIndex] == line)
                return;
        }

        History[_historyNext] = line;
        _historyNext = (_historyNext + 1) % HistoryCapacity;

        if (_historyCount < HistoryCapacity)
            _historyCount++;
    }

    /// <summary>
    /// Moves the history cursor by <paramref name="direction"/> (-1 for older/Up,
    /// +1 for newer/Down) and redraws the input line with the selected entry.
    /// </summary>
    private static void NavigateHistory(int direction)
    {
        if (_historyCount == 0)
            return;

        int newCursor;

        if (_historyCursor < 0)
        {
            // Not currently browsing. Up starts at the most recent entry;
            // Down does nothing (nothing "newer" than a fresh line).
            if (direction > 0)
                return;

            newCursor = _historyCount - 1;
        }
        else
        {
            newCursor = _historyCursor + direction;

            if (newCursor  < 0)
                newCursor = 0;
            else if (newCursor >= _historyCount)
            {
                // Moved past the newest entry - return to a fresh empty line.
                _historyCursor = -1;
                ReplaceInputLine(string.Empty);
                return;
            }
        }

        _historyCursor = newCursor;

        var oldestIndex = _historyCount < HistoryCapacity ? 0 : _historyNext;
        var absoluteIndex = (oldestIndex + newCursor) % HistoryCapacity;
        ReplaceInputLine(History[absoluteIndex]);
    }

    /// <summary>
    /// Clears the currenty-edited line on screen and replaces it with
    /// <paramref name="newLine"/>, keeping _inputBuffer in sync.
    /// </summary>
    private static void ReplaceInputLine(string newLine)
    {
        while (_inputLenght > 0)
            RemoveLastChar();

        for (var i = 0; i < newLine.Length; i++)
            AppendChar(newLine[i]);
    }

    private static void AppendChar(char c)
    {
        // Ignore non-printable / control characters we dont't explicitly handle.
        if (c < 0x20)
            return;

        if (_inputLenght >= _inputBuffer.Length)
            return; // buffer full - silently drop, no realloc on bare-metal hot path

        _inputBuffer[_inputLenght] = c;
        _inputLenght++;
        Console.Write(c);
    }

    private static void RemoveLastChar()
    {
        if (_inputLenght == 0)
            return;

        _inputLenght--;
        // Move cursor back, overwrite with space, move back again.
        Console.Write('\b');
        Console.Write(' ');
        Console.Write('\b');
    }

    private static void WritePrompt()
    {
        Console.Write(Prompt);
    }

    // ---------------------------------------------------------------
    // Parsing & dispatch
    // ---------------------------------------------------------------

    private static void Dispatch(string line)
    {
        var tokens = Tokenize(line);

        if (tokens.Length == 0)
            return;

        var commandName = tokens[0];
        var args = new string[tokens.Length - 1];
        Array.Copy(tokens, 1, args, 0, args.Length);

        var index = IndexOfCommand(commandName);

        if (index >= 0)
            CommandList[index].Handler(args);
        else
        {
            Console.WriteLine("Unknown command: " + commandName);
            Console.WriteLine("Type 'help' for a list of commands.");
        }
    }

    /// <summary>
    /// Simple whitespace tokenizer. No quoting support yet - deferred until
    /// Drill needs to handle arguments containing spaces (later phase).
    /// </summary>
    private static string[] Tokenize(string line)
    {
        var tokens = new List<string>();
        var start = -1;

        for (var i = 0; i < line.Length; i++)
        {
            var isSpace = line[i] == ' ' || line[i] == '\t';

            if (!isSpace && start < 0)
                start = i;
            else if (isSpace && start >= 0)
            {
                tokens.Add(line.Substring(start, i - start));
                start = -1;
            }
        }

        if (start >= 0)
            tokens.Add(line.Substring(start, line.Length - start));

        return tokens.ToArray();
    }

    // ---------------------------------------------------------------
    // Built-in commands
    // ---------------------------------------------------------------

    private static void RegisterBuiltins()
    {
        RegisterCommand("help", "Lists available commands.", OnHelp);
        RegisterCommand("clear", "Clears the screen.", OnClear);
        RegisterCommand("echo", "Prints back the given arguments.", OnEcho);
        RegisterCommand("history", "Lists previous entered commands.", OnHistory);
    }

    private static void OnHelp(string[] args)
    {
        Console.WriteLine("Available commands:");
        for (var i = 0; i < CommandList.Count; i++)
        {
            var command = CommandList[i];
            Console.WriteLine($"  {CommandList[i].Name} - {CommandList[i].Description}");
        }
    }

    private static void OnClear(string[] args)
    {
        Console.Clear();
    }

    private static void OnEcho(string[] args)
    {
        // Mosa.Korlib (the corlib actually pulled in via Mosa.Runtime on the
        // BareMetal target) does not implement string.Join - only
        // string.Concat and string.Format overloads (no separator support).
        // string.Join exists solely in Mosa.TinyCoreLib, an alternative
        // corlib this project does not reference - same cathegory of gap
        // already documented for Dictionary<TKey, TValue> above. Joining
        // manually with a loop instead.
        var joined = string.Empty;

        for (var i = 0; i < args.Length; i++)
        {
            if (i > 0)
                joined += " ";

            joined += args[i];
        }

        Console.WriteLine(joined);
    }

    private static void OnHistory(string[] args)
    {
        if (_historyCount == 0)
        {
            Console.WriteLine("(empty)");
            return;
        }

        var oldestIndex = _historyCount < HistoryCapacity ? 0 : _historyNext;

        for (var i = 0; i < _historyCount; i++)
        {
            var absoluteIndex = (oldestIndex + i) % HistoryCapacity;
            Console.WriteLine("  " + (i + 1) + "  " + History[absoluteIndex]);
        }
    }
}
