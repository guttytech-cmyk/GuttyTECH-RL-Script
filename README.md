<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&color=0A0A0A&height=120&section=header&text=ROCKET%20LEAGUE%20GUTTYTECH&fontSize=36&fontColor=E50A0A&animation=fadeIn" alt="RL GuttyTECH" />

[![Version](https://img.shields.io/badge/Version-v25.0.5-E50A0A?style=for-the-badge)](https://github.com/guttytech-cmyk/GuttyTECH-RL-Script/releases/tag/v25.0.5)
[![Platform](https://img.shields.io/badge/Platform-Windows_10%20%7C%2011-0078D4?style=for-the-badge&logo=windows)](https://guttytech.com)
[![UI](https://img.shields.io/badge/UI-WPF_.NET_9-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com)
[![Anti-Cheat](https://img.shields.io/badge/Easy_Anti--Cheat-safe-00C853?style=for-the-badge)](https://www.easy.ac)

**App WPF · COMPLETO / CRIADOR · sync de vídeo no `.save` · watcher · recuperação · 1 arquivo `.exe`**

[Download v25.0.5](https://github.com/guttytech-cmyk/GuttyTECH-RL-Script/releases/latest) · [Website](https://guttytech.com) · [Comunidade](https://guttytech.com/comunidade)

</div>

---

## Início rápido

1. Baixe **`GuttyTECH_RL.exe`** na aba [Releases](https://github.com/guttytech-cmyk/GuttyTECH-RL-Script/releases/latest).
2. Feche o Rocket League.
3. Clique direito → **Executar como administrador** (obrigatório).
4. Na primeira vez o SmartScreen pode avisar → *Mais informações* → *Executar assim mesmo*.
5. Escolha **COMPLETO** ou **CRIADOR**.

| Página | O que faz |
|--------|-----------|
| **Visão Geral** | Modo ativo, proteção (watcher), caminho do INI, atalhos |
| **Otimização** | Aplica **COMPLETO** ou **CRIADOR** |
| **Recuperação** | Permissões, reparar perfil, boot, save, EAC 30005, Corrigir Tudo |
| **Sistema** | Remover otimização, launch options, pacote de suporte (ZIP) |

> Detalhe técnico (chaves do INI, sync, watcher): [**DESCRICAO.md**](DESCRICAO.md)

---

## Modos

- **COMPLETO** — FPS máximo. Texturas até **2×2**, sombras/efeitos no mínimo, menu de vídeo sincronizado no `.save`, watcher anti-rewrite quando o jogo fecha.
- **CRIADOR** — visual legível pra stream/clip + cortes invisíveis de FPS (MSAA/Apex/foliage pesado, etc.). FPS uncapped.
- **Remover** — volta ao stock limpo, limpa modo/tag/watcher e destrava o perfil.

O INI **não** fica travado em somente-leitura (de propósito): o menu de vídeo in-game precisa escrever. A proteção é o **watcher** (card PROTEÇÃO).

---

## O que o app faz

- Localiza `TASystemSettings.ini` (Documents / OneDrive / perfis).
- Backup automático em `%USERPROFILE%\GuttyTECH\RL-Optimizer-v22\Backups\`.
- Preserva resolução e modo de tela (`ResX` / `ResY` / fullscreen / borderless).
- Sincroniza opções de vídeo no `.save` (Epic e Steam).
- Copia launch options (clipboard + `GuttyTECH-RL-LaunchCommand.txt` no Desktop).
- Gera **pacote de suporte ZIP** pra enviar no chat (diagnóstico, INI, logs, EAC, saves).

**Não mexe** em Windows, registro, rede, HPET nem TCP. Ring-0 antigo ficou em [`legacy/`](legacy/).

### Launch options (recomendado)

```
-nomovie -NOSPLASH -nomansky +mat_antialias 0 -high
```

Cole nas opções de inicialização da Steam ou nos argumentos da Epic. Se der stutter/áudio estranho, tire o `-high`.

---

## Benchmarks — CapFrameX

> Medição histórica do stack **V21 OMEGA** (Ring-0 + INI, hoje em [`legacy/`](legacy/)). A linha **v25** entrega a camada de **INI + save + watcher** desses ganhos, sem tocar no sistema.

**Hardware:** Intel Core i9-12900KF · NVIDIA GeForce RTX 4090

| Métrica | Antes | Depois | Ganho |
|---------|------:|-------:|------:|
| Média FPS | 608.72 | **800.41** | **+31.5%** |
| Mediana | 629.33 | **833.82** | **+32.5%** |
| 1% Low | 354.57 | **443.52** | **+25.1%** |
| 0.1% Low | 240.99 | **262.11** | **+8.8%** |
| Máximo | 1,641.77 | **3,060.91** | +86.4% |

<div align="center">

![Bar Charts](bar_charts.png)
![Frame Time](frame_time.png)

</div>

---

## Requisitos

- Windows 10 / 11
- **Administrador** (manifest + gate no arranque)
- Rocket League (Steam ou Epic)
- Não precisa instalar .NET no PC do cliente (single-file)

---

## Segurança

- Só config do jogo (`TASystemSettings.ini` + sync de vídeo no save) — sem patch de binário.
- Backup antes de cada alteração; rollback via **Remover** / recuperação.
- Flags de launch compatíveis com Easy Anti-Cheat.
- `legacy/` com tweaks Ring-0: use por conta própria.

---

## Changelog recente

- **v25.0.5** — detecção de modo/admin, recuperação, clipboard elevado, ZIP de suporte  
- **v25.0.1** — reparar EAC 30005, Completo/Criador FPS, watcher  
- **v25.0.0** — UI WPF com motion + startup robusto  

Histórico completo: [CHANGELOG.md](CHANGELOG.md) · [Releases](https://github.com/guttytech-cmyk/GuttyTECH-RL-Script/releases)

---

<div align="center">

[![GuttyTECH](https://img.shields.io/badge/GuttyTECH-guttytech.com-E50A0A?style=for-the-badge)](https://guttytech.com)

<img src="https://capsule-render.vercel.app/api?type=waving&color=E50A0A&height=70&section=footer&fontSize=14&fontColor=0A0A0A" alt="footer" />

</div>
