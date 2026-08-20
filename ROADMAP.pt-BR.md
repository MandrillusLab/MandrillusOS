*Leia em outros idiomas: [English](ROADMAP.md)*

# Roadmap do Mandrillus

Este documento acompanha a trajetória de desenvolvimento do **Mandrillus OS** e do ecossistema mais amplo **Mandrillus Systems** — um pequeno conjunto de aplicações construído em torno de um sistema operacional nativo em C#, desenvolvido com o [MOSA Project](https://github.com/mosa/MOSA-Project).

Legenda de status: ✅ Concluído · 🚧 Em andamento · ⏳ Planejado · 💭 Conceito

---

## Fase 0 — Fundação

- ✅ Estrutura da solution: `Mandrillus.Kernel` (agnóstico de plataforma) + `Mandrillus.Kernel.x86` (alvo x86)
- ✅ Scaffolding via template MOSA (`mosakrnl`)
- ✅ Pacotes MOSA fixados na versão `2.6.1.1669` (workaround para regressão de empacotamento em 2.6.1.1694+)
- ✅ Boot validado em QEMU (via Mosa.Tool.Launcher)
- ✅ Boot validado em Hyper-V (Generation 1, Secure Boot desabilitado, conversão manual `.img` → `.vhd`)
- ✅ Licenciamento: MIT (Mandrillus Systems) + `THIRD-PARTY-LICENSES.md` (New BSD do MOSA)
- ✅ README bilíngue (`README.md` / `README.pt-BR.md`)

## Fase 1 — Núcleo do Kernel (x86, 32 bits)

Rastreado como Issues no GitHub, sob o milestone **"Phase 1 — Kernel Core"**.

> **Nota:** uma investigação confirmou (via [documentação do MOSA](https://www.mosa-project.org/a-dive-into-baremetal.html) e o log de boot real) que a maior parte da infraestrutura de baixo nível da Fase 1 já é fornecida pelo kernel MOSA BareMetal sobre o qual o `Mandrillus.Kernel.x86` é construído. Os itens abaixo marcados como *(herdado)* não são trabalho autoral — estão documentados aqui por rastreabilidade e para deixar a arquitetura explícita.

- ✅ Configuração da GDT (Global Descriptor Table) *(herdado do MOSA BareMetal)* — #1
- ✅ IDT (Interrupt Descriptor Table) + tratamento básico de interrupções *(herdado do MOSA BareMetal)* — #2
- ✅ Gerenciador de memória física (alocador de páginas) *(herdado do MOSA BareMetal)* — #3
- ✅ Memória virtual / paginação *(herdado do MOSA BareMetal)* — #4
- ⏳ Interrupção de timer (PIT/IRQ0) — investigação em andamento, pode também ser herdado — #5
- ✅ Driver de teclado *(herdado do MOSA BareMetal — `StandardKeyboard`)* — #6
- ✅ Driver de vídeo em modo texto *(herdado do MOSA BareMetal — API `Console`)* — #7
- ⏳ Shell interativo mínimo (trabalho autoral — integra teclado + saída de console em um loop de comandos) — #8

## Fase 2 — Serviços de Sistema

- 💭 Gerenciamento de processos/tarefas
- 💭 Escalonador básico (cooperativo → preemptivo)
- 💭 Comunicação entre processos (IPC)
- 💭 Suporte a sistema de arquivos (começar somente leitura, FS simples)
- 💭 Camada de abstração de dispositivos

## Fase 3 — Portabilidade para 64 bits

- 💭 Avaliação do alvo x64 assim que o kernel x86 tiver escalonador + gerenciador de memória + drivers
- 💭 Transição para long mode
- 💭 Paginação de 4 níveis
- 💭 Revalidação da cadeia de boot (configuração do Launcher, tratamento de formato de imagem) para x64

## Fase 4 — Ecossistema Mandrillus Systems (Aplicações)

Trabalho posterior aos marcos do kernel — requer um modelo de processos e sistema de arquivos funcionais.

- 💭 Editor de texto simples
- 💭 Editor de código
- 💭 Aplicação simples de desenho
- 💭 Pacote office nativo e leve

---

## Notas

- Ferramentas: `Mosa.Tool.Launcher` (GUI) e `Mosa.Tool.Launcher.Console` (CLI, flags de traço único, ex: `-platform x86`, `-emulator qemu`, `-destination <path>`).
- Plataforma alvo para as Fases 0–2: **x86 (32 bits)** — escolhida pela maturidade do toolchain, paginação mais simples e configuração de modo protegido melhor documentada dentro do MOSA.
- Tagline: *"Mandrillus Systems — evolution, one bit at a time."*
- **Nota de arquitetura:** o `Mandrillus.Kernel.x86` é construído sobre o **kernel MOSA BareMetal** (`Mosa.Kernel.BareMetal.x86.dll` / `Mosa.Kernel.BareMetal.dll`, referenciado via os pacotes `Mosa.Platform.x86` e `Mosa.Platform`). O BareMetal cuida de GDT, IDT, gerenciamento de memória física/virtual, primitivas de escalonamento, HAL e registro de drivers de dispositivo (incluindo teclado e saída de vídeo/console) automaticamente durante o boot, antes de qualquer código específico do Mandrillus ser executado. O trabalho autoral do Mandrillus começa na camada de aplicação — o shell interativo em diante. Veja o `README.md` para uma divisão clara entre componentes herdados e autorais.
