# Versionamento e workflow de Git

[← voltar ao CLAUDE.md](../../CLAUDE.md)

## Versionamento (SemVer)

Esquema: `MAJOR.MINOR.PATCH`, pré-1.0 (`0.x.x` = ainda instável, API pode quebrar livremente).

- **MINOR** sobe quando uma Issue do roadmap representando uma feature completa é fechada (ex.: #8 Drill shell → `0.1.0`; #9 PIT timer → `0.2.0`)
- **PATCH** sobe para correções dentro de uma MINOR já lançada
- **MAJOR** reservado para `1.0.0` (primeiro marco completo do kernel + ecossistema mínimo de apps) ou uma quebra de compatibilidade futura

**Camadas de versão no projeto:**

- **Mandrillus OS** (o produto) — versão central, hoje em `v0.2.0` (após o fechamento da Issue #9)
- **Drill** (componente/shell) — não tem versão própria pública por enquanto; só passaria a ter se um dia virar projeto separado (`Mandrillus.Drill`, ver adiamento em [status.md](status.md))
- **Build/compilação** — número técnico separado, adiado até haver CI real (ver abaixo)

### Onde vive a versão no código

Centralizada em `MandrillusVersion.cs` (namespace `Mandrillus.Kernel`), não hardcoded direto no `Console.WriteLine` do `Program.cs`. O comentário de cabeçalho da classe já documenta o processo manual — ver arquivo no repositório para o texto completo.

### Processo (100% manual por enquanto — sem CI)

1. Ao fechar uma Issue que representa feature completa: bump manual em `MandrillusVersion.cs` como parte do mesmo PR
2. Após merge do PR em `master`: tag o commit —

   ```powershell
   git tag v0.1.0
   git push origin v0.1.0
   ```

3. Build metadata (`0.1.0+42`, número de commit/CI) fica **adiado deliberadamente** — só faz sentido com pipeline de CI configurado; não vale a complexidade agora

## Branches e PR/merge

- **Modelo:** Trunk-based / GitHub Flow. `master` sempre estável e bootável.
- **Branches de feature:** `feature/<numero-issue>-<nome-curto>`
- **Merge:** squash merge **apenas via PR** — sem merge direto em `master`, mesmo sendo projeto solo
- **Proteção de branch (GitHub ruleset):**
  - PR obrigatório
  - 1 aprovação obrigatória
  - Squash-only
  - Resolução de conversas obrigatória antes do merge
  - Sem exceção para admins (nem para o próprio Leandro)
- **Origem da regra:** um PR de spam automatizado (#11, bot) foi recebido e fechado sem merge — a proteção de branch foi criada em resposta a esse evento, não é só formalidade preventiva teórica

### ⚠️ Armadilha confirmada: resolver conflito pelo navegador vs. localmente

Se um `git rebase`/`git merge` local ficar com conflito pendente numa branch, e o conflito acabar sendo resolvido **pela interface web do GitHub** (editor de conflito do PR) em vez de terminar a resolução local, o repositório **local continua com o rebase/merge marcado como em andamento** — o Git não sabe que o remoto já resolveu.

Sintoma: comandos como `git checkout <outra-branch>` ou `git stash pop` falham com `needs merge` / `you need to resolve your current index first`, mesmo o PR já estando mergeado no GitHub.

**Correção segura:** confirmar primeiro que o merge realmente terminou no remoto (`git log master --oneline` depois de `git pull`, procurando o commit de squash), e só então:

```powershell
git rebase --abort    # ou git merge --abort, dependendo de qual estava em andamento
```

Isso descarta com segurança o estado local pendente sem perder nada, porque o resultado real já está seguro no `master` remoto.

**Regra prática:** ao resolver conflito de um PR pela web do GitHub, sempre voltar ao terminal depois e abortar qualquer rebase/merge local que tenha ficado pendente daquela mesma branch, antes de tentar trocar de branch ou aplicar stash.

## Notas

- Autoria/revisão de todo PR continua manual — nenhuma ferramenta de IA abre, aprova ou faz merge de PR sozinha (consistente com a decisão de não usar modo agent, ver [CLAUDE.md](../../CLAUDE.md))
- Tags de versão (`vX.Y.Z`) devem corresponder exatamente ao valor em `MandrillusVersion.cs` no commit taggeado — checar antes de criar a tag
- O Copilot code review automático (configurado no repositório) roda em todo PR e pode gerar comentários; sob a regra "resolução de conversas obrigatória", esses comentários **bloqueiam o merge** até serem marcados como resolvidos — mesmo sendo de uma IA, vale ler antes de resolver, já que podem apontar erros reais (ex.: caminho de arquivo incorreto em uma citação)
