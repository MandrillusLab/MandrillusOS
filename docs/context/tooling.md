# Tooling, versionamento e convenções

[← voltar ao CLAUDE.md](../../CLAUDE.md)

## Versionamento de dependências {#versionamento}

Pacotes NuGet MOSA pinados em **`2.6.1.1669`** — `Mosa.Platform`, `Mosa.Platform.x86`, `Mosa.DeviceSystem`, `Mosa.Tools.Package`. Builds `2.6.1.1694+` têm regressão de empacotamento quebrando resolução de `Mosa.Compiler.Platforms`. Nunca sugerir upgrade sem avisar disso.

**Regressão CONFIRMADA com evidência direta (não mais só inferida por data):** testado atualizando os 4 pacotes via NuGet Package Manager do Visual Studio (`Mosa.Platform`/`Mosa.DeviceSystem`/`Mosa.Platform.x86` → `2.6.1.1698`; `Mosa.Tools.Package` → `2.6.1.1724`, a mais recente disponível em ambos os casos). Build/restore passaram normalmente, mas rodar `Mosa.Tool.Launcher.Console.exe` falhou com:

```
System.IO.FileNotFoundException: Could not load file or assembly 'Mosa.Compiler.Platforms, Version=2.6.1.0, Culture=neutral, PublicKeyToken=null'.
   at Mosa.Tool.Launcher.Console.Program.RegisterPlatforms() in .../Program.cs:line 101
   at Mosa.Tool.Launcher.Console.Program.Main(String[] args) in .../Program.cs:line 19
```

**Causa raiz confirmada via inspeção da fonte do MOSA:** `Mosa.Tool.Launcher.Console.csproj` referencia `Mosa.Compiler.Platforms` via `ProjectReference` — dependência interna de build do próprio MOSA, não um pacote NuGet separado. Ao empacotar `Mosa.Tools.Package` para publicação no NuGet, esse assembly precisa ser copiado para dentro do pacote; esse passo de empacotamento aparentemente falha ou é omitido em algumas versões publicadas, deixando o `.exe` com uma referência de assembly que não resolve em runtime.

**Confirmado que é a mesma regressão que motivou o pin original** (não uma nova) — persiste até `2.6.1.1724` (testado meses depois da suspeita original em `1694`). Revertendo os 4 pacotes para `2.6.1.1669` no Visual Studio, o Launcher voltou a funcionar normalmente — confirmado por Leandro.

**Hipótese descartada durante a mesma investigação:** a suspeita de que o Launcher (`Mosa.Tools.Package`) estivesse desatualizado em relação ao core não se sustentou — na verdade `Mosa.Tools.Package` (`2.6.1.1724`, publicado 15/05/2026) estava **mais recente** que `Mosa.Platform.x86` (`2.6.1.1698`, publicado 27/04/2026) no momento do teste. A regressão de empacotamento não tem relação com dessincronia de versão entre pacotes.

**Não há barreira de `TargetFramework` para testar versões novas:** tanto `Mandrillus.Kernel.csproj` quanto `Mandrillus.Kernel.x86.csproj` já usam `net10.0`/`LangVersion 14.0`, mesmo pinados em `2.6.1.1669` — as versões mais recentes do MOSA (`Platform.x86` `2.6.1.1698`+) também já são `net10.0`. O único bloqueio real é o bug de empacotamento do `Mosa.Compiler.Platforms` acima, não incompatibilidade de plataforma-alvo.

**Achado relacionado (mesma investigação):** os 4 gaps do Korlib já documentados (`Dictionary`, `string.Join`, `Array.Copy`, `DateTime.Now` — ver [constraints.md](constraints.md#korlib)) continuam presentes no `master` atual do MOSA (commit de maio/2026). Manter o pin não significa perder nenhuma correção desses gaps.

**Reportado upstream:** [Issue #1295](https://github.com/mosa/MOSA-Project/issues/1295) no `mosa/MOSA-Project`, aberta em 28/08/2026 — ver lista consolidada de contribuições upstream em [constraints.md](constraints.md#contribuicoes-upstream).

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
