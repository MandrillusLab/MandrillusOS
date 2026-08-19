# Third-Party Licenses / Licenças de Terceiros

*(EN)* This file lists third-party licenses that apply to dependencies of this project. The license texts themselves are kept in their original English wording, as required for legal accuracy.

*(PT-BR)* Este arquivo lista as licenças de terceiros aplicáveis às dependências deste projeto. Os textos das licenças em si são mantidos no idioma original (inglês), conforme exigido para precisão jurídica.

---

## MOSA Project

Mandrillus OS is built on top of the [MOSA Project](https://github.com/mosa/MOSA-Project), used as the compilation toolchain (IL → native code via AOT) and as the base for kernel templates. MOSA's code itself is not modified in this repository — it's consumed as a dependency (NuGet packages `Mosa.Platform`, `Mosa.Platform.x86`, `Mosa.DeviceSystem`, `Mosa.Tools.Package`).

*(PT-BR: O Mandrillus OS é construído sobre o MOSA Project, usado como toolchain de compilação e base dos templates de kernel. O código do MOSA não é modificado neste repositório — é consumido como dependência via NuGet.)*

The MOSA Project is licensed under the **New BSD License** (BSD 3-clause), reproduced below as required by the license terms.

---

## MOSA Project — New BSD License

```
Copyright (c) MOSA Project

All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the MOSA Project nor the names of its
      contributors may be used to endorse or promote products derived
      from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.
```

Fonte: [mosa-project.org/license.html](https://www.mosa-project.org/license.html) · [github.com/mosa/MOSA-Project](https://github.com/mosa/MOSA-Project)

---

## Libraries used internally by MOSA / Bibliotecas usadas internamente pelo MOSA

MOSA itself depends on third-party libraries, listed here for transparency (not used directly by Mandrillus code, only as part of MOSA's own toolchain):

*(PT-BR: O próprio MOSA depende de bibliotecas de terceiros, listadas aqui por transparência — não são usadas diretamente pelo código do Mandrillus, apenas fazem parte da toolchain do MOSA.)*

| Library | License | Use |
|---|---|---|
| DockPanel Suite (WeiFen Luo) | MIT | Panel docking in the MOSA debugger |
| dnlib | MIT | Type system of the MOSA compiler |
| SharpDisasm | Simplified BSD | Debugging and diagnostics |
| xUnit.Net | Apache License | MOSA compiler test suite |
| Farm-Fresh Web Icons | CC Attribution 3.0 | MOSA Tool GUI icons |
