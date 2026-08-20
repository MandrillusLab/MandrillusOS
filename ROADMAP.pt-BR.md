*Leia em outros idiomas: [English](ROADMAP.en.md)*

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

## Fase 1 — Núcleo do Kernel (x86, 32 bits)

Rastreado como Issues no GitHub, sob o milestone **"Phase 1 — Kernel Core"**.

- ⏳ Configuração da GDT (Global Descriptor Table) — #1
- ⏳ IDT (Interrupt Descriptor Table) + tratamento básico de interrupções — #2
- ⏳ Gerenciador de memória física (alocador de páginas) — #3
- ⏳ Memória virtual / paginação — #4
- ⏳ Interrupção de timer (PIT/IRQ0) — #5
- ⏳ Driver de teclado (IRQ1) — #6
- ⏳ Driver de vídeo em modo texto (VGA) — #7
- ⏳ Shell interativo mínimo (boot + interrupções + teclado + saída integrados) — #8

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

---

2026 *"Mandrillus Systems — evolution, one bit at a time."*
