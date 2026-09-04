// Copyright (c) MOSA Project. Licensed under the New BSD License.
// Copyright (c) 2026 Leandro Vieira / Mandrillus Systems
// Licensed under the MIT License. See LICENSE file in the project root.
using System;
using Mandrillus.Kernel.Hardware;
using Mandrillus.Kernel.Shell;
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
		Console.WriteLine("Copyright (c) MOSA Project.");
		Console.WriteLine("Licensed under the New BSD License.");
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"Mandrillus OS Kernel v{MandrillusVersion.GetDisplayVersion()}");
		Console.WriteLine($"Copyright (c) {MandrillusVersion.CopyrightYear} Mandrillus Systems.");
		Console.WriteLine("Licensed under the MIT License.");
        Console.ResetColor();

		HardwareSetup.RegisterPitTimer();

        Drill.Start();

        for ( ; ; )
		{ }
	}
}
