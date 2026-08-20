// Copyright (c) MOSA Project. Licensed under the New BSD License.
// Copyright (c) Mandrillus OS. Licensed under the MIT License.
using Mosa.Kernel.BareMetal;

namespace Mandrillus.Kernel.x86;

public static class Boot
{
	public static void Main()
	{
		Debug.WriteLine("Boot::Main()");
		Debug.WriteLine("MOSA x86 Kernel");

		Program.EntryPoint();
	}
}
