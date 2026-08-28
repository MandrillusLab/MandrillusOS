# Mandrillus OS — Contexto para IA (modo leitura/chat)

> Resumo de decisões técnicas e de arquitetura tomadas manualmente por Leandro Vieira
> ao longo do desenvolvimento do Mandrillus OS, organizado para acelerar o contexto de
> qualquer IA usada em modo chat/planejamento (Claude Code, Copilot Chat etc.).
> Não é instrução para agir de forma autônoma — nenhuma ferramenta edita este projeto
> sozinha. Toda escrita de código é manual, por escolha deliberada. Este arquivo serve
> para embasar explicações, diagnóstico de bugs e discussão de impacto antes da implementação.

## Leia primeiro
- **O que é o projeto, arquitetura, como rodar:** [README.md](README.md)
- **Fases, issues, progresso:** [ROADMAP.md](ROADMAP.md)

## As 3 regras que mais importam
1. **Scheduler é cooperativo, não preemptivo.** Nada de chamadas bloqueantes no loop principal (`Console.ReadLine()` é proibido; usar polling não-bloqueante + `HAL.Yield()`). Detalhes: [docs/context/constraints.md](docs/context/constraints.md#scheduler)
2. **`Mosa.Korlib` é menor que o BCL padrão.** `Dictionary<TKey,TValue>` e `string.Join` não existem — ver workarounds e o padrão para novos gaps em [docs/context/constraints.md](docs/context/constraints.md#korlib)
3. **NuGet MOSA pinado em `2.6.1.1669`** — não sugerir upgrade sem avisar da regressão de empacotamento. Detalhes: [docs/context/tooling.md](docs/context/tooling.md#versionamento)

## Atribuição de pesquisa
Quando uma decisão de design é informada por uma fonte externa específica (código-fonte de outro projeto, discussão pública, implementação de referência), ela é citada diretamente no README/ROADMAP com a fonte e o porquê de não ter sido apenas copiada — nunca apresentada como se tivesse sido alcançada isoladamente. Ver exemplo real: [README.md#design-references](README.md#design-references-credit-where-its-due). Uma IA discutindo uma decisão técnica deste projeto deve seguir o mesmo padrão ao explicar de onde a informação vem.

## Onde estamos agora
Fase 1 quase fechada. Único entregável original restante: **Issue #8 (shell Drill)**, em progresso ativo, sendo digitada manualmente — uma IA não deve gerar o arquivo inteiro, só discutir trechos.

Mandrillus também é usado para praticar Git disciplinado (branches, PR mesmo solo, squash merge). Testes automatizados (xUnit) **não** fazem parte do escopo deste projeto — decisão fechada, ver [docs/context/constraints.md](docs/context/constraints.md#testes-automatizados).

Estado completo, decisões fechadas e itens deliberadamente adiados: [docs/context/status.md](docs/context/status.md)

## Referência detalhada
- [docs/context/constraints.md](docs/context/constraints.md) — limitações de linguagem/runtime, restrição de scheduler, fronteira platform-agnostic vs. x86
- [docs/context/tooling.md](docs/context/tooling.md) — versões de dependências (MOSA), emuladores, comandos, convenção de copyright
- [docs/context/versioning.md](docs/context/versioning.md) — versionamento SemVer do próprio Mandrillus, branches, PR/merge, proteção de branch
- [docs/context/status.md](docs/context/status.md) — fase atual, arquitetura do Drill, decisões fechadas (PIT/Issue #9), itens adiados

---
*Mantido manualmente por Leandro. Atualizar ao fechar uma Issue ou decisão de design relevante. Em caso de divergência, README/ROADMAP prevalecem como documentação pública.*
