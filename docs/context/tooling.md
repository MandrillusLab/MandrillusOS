# Tooling, versionamento e convenções

[← voltar ao CLAUDE.md](../../CLAUDE.md)

## Versionamento de dependências {#versionamento}

Pacotes NuGet MOSA pinados em **`2.6.1.1669`** — `Mosa.Platform`, `Mosa.Platform.x86`, `Mosa.DeviceSystem`, `Mosa.Tools.Package`. Builds `2.6.1.1694+` têm regressão de empacotamento quebrando resolução de `Mosa.Compiler.Platforms`. Nunca sugerir upgrade sem avisar disso.

> Nota: o README público cita a versão mais recente do MOSA "no momento da escrita" (`2.6.1.1724` na última verificação) — esse número pode ficar desatualizado com o tempo; conferir contra o MOSA real antes de citar como atual.

Template: `dotnet new mosakrnl` (via `dotnet new install Mosa.Templates`)

## Emuladores

Validados: QEMU (nativo via Launcher) e Hyper-V Geração 1 (Secure Boot desligado).

Hyper-V exige conversão manual `.img` → `.vhd`:
```
qemu-img convert -f raw -O vpc
```
(dropdown VHD do Launcher está quebrado na 1669)

Referência completa de flags do `Mosa.Tool.Launcher.Console`: ver `MOSA-Launcher-CLI-Reference.md` no repositório.

## Convenção de copyright

- Cabeçalho só em arquivos-âncora (`Program.cs`, `Boot.cs`-nível de entry point) — não em todo arquivo
- Formato:
  ```
  // Copyright © [ano] Leandro Vieira / Mandrillus Systems
  // Licensed under the MIT License. See LICENSE file in the project root.
  ```
- Ano = quando aquele arquivo específico foi criado (não a data de fundação 2019 da marca)
- `THIRD-PARTY-LICENSES.md` preserva as datas originais de copyright do MOSA (New BSD License) intocadas

## Git / workflow / versionamento

Movido para [versioning.md](versioning.md) — cobre branches, PR/merge, proteção de branch e SemVer do próprio Mandrillus.
