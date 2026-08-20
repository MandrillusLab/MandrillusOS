// Copyright (c) MOSA Project. Licensed under the New BSD License.

using System;
using Mosa.Kernel.BareMetal;
using Mosa.Runtime.Plug;

namespace Mandrillus.Kernel;

public static class Program
{
	[Plug("Mosa.Runtime.StartUp::BootOptions")]
	public static void SetBootOptions()
	{
		BootSettings.EnableDebugOutput = true;
		BootSettings.EnableVirtualMemory = true;
		BootSettings.EnableMinimalBoot = true;
	}

	public static void EntryPoint()
	{
		Debug.WriteLine("Program::EntryPoint()");

		Console.ResetColor();
		Console.Clear();
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine("MOSA x86 Kernel");
        Console.ForegroundColor= ConsoleColor.Blue;
        Console.WriteLine("2026 - Mandrillus OS Kernel v0.0.1");
		Console.ResetColor();
		Console.WriteLine("Hello World!");

        for (; ; )
		{ }
	}
}
