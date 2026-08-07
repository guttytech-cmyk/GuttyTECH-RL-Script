# GUTTYTECH — Rocket League Optimizer v25.0.5

> **O que é:** app WPF (`.exe` único) que otimiza o `TASystemSettings.ini` do Rocket League (UE3), sincroniza o menu de vídeo nos `.save` e mantém um watcher anti-rewrite.  
> **O que NÃO é:** não mexe no Windows, registro, rede, HPET nem TCP. Isso ficou no script antigo em [`legacy/RL_GUTTYTECH_v21.5.bat`](legacy/RL_GUTTYTECH_v21.5.bat).

**Download:** [`GuttyTECH_RL.exe`](https://github.com/guttytech-cmyk/GuttyTECH-RL-Script/releases/latest) — **Executar como administrador**.

| Página | Função |
|--------|--------|
| Visão Geral | Estado do modo, PROTEÇÃO (watcher), caminho do INI |
| Otimização | **COMPLETO** ou **CRIADOR** |
| Recuperação | Permissões, reparar perfil, boot, save, EAC 30005, Corrigir Tudo |
| Sistema | Remover, launch options, ZIP de suporte |

---

## 1. O que o app faz de verdade

- **Acha o INI sozinho** — `Documents`, OneDrive e varredura de perfis.
- **Bloqueia apply com o jogo aberto** — o RL reescreve o `.ini` ao fechar.
- **Backup** — `%USERPROFILE%\GuttyTECH\RL-Optimizer-v22\Backups\` (+ original na 1ª vez).
- **Destrava permissões** — read-only / ACL / takeown quando necessário.
- **Aplica template + CompletoForce / CriadorForce** — reclampa chaves críticas depois do jogo reescrever.
- **Preserva resolução e borda** — `ResX`, `ResY`, `Fullscreen`, `Borderless`, `AutoDetectDesktopResolution`.
- **INI gravável** — **não** trava somente-leitura (o menu de vídeo precisa escrever). Proteção = **watcher**.
- **Sync de vídeo no `.save`** — Epic (`SaveDataEpic`) e Steam (`SaveData`).
- **Marcador de modo** — `GuttyTechMode` + fingerprint + tag local.
- **Admin obrigatório** — manifest + TokenElevation no arranque.
- **Clipboard elevado** — comando de launch também grava `Desktop\GuttyTECH-RL-LaunchCommand.txt`.
- **ZIP de suporte** — diagnóstico, INI, logs, EAC, saves, launch command, watcher/tag.

---

## 2. Frame pacing (COMPLETO e CRIADOR)

| Config | Valor | Prática |
|--------|:-----:|---------|
| `UncappedFramerate` | **True** | Sem teto artificial do engine |
| `bSmoothFrameRate` | **False** | Sem “suavizar” FPS |
| `UseVsync` | **False** | Menos input lag |
| `WaitForGPU` | **False** | Boot-safe / sem hang |
| `OnlyStreamInTextures` | **False** | Boot-safe (True travava boot) |
| `OneFrameThreadLag` | **True** | Estabilidade UE3 |
| `AllowPerFrameSleep` / `Yield` | **True** | Pacing estável |
| `CustomFPS` | **0** | Sem cap custom no INI |
| `MaxFilterBlurSampleCount` | **1** | Amostras do blur de filtro UE3. **0 crasha**. Valor aprovado |
| `ScreenPercentage` | **100** | Sem borda preta por scale errado |

> FPS real = INI + menu Vídeo (sync automático) + GPU. Launch options quase não mudam FPS.

---

## 3. COMPLETO — batata competitiva

Validado estável (LOD 2×2):

- Texturas **MaxLODSize=2**, `LODBias=100`, filtro **Point**, aniso **0**
- `DetailMode=0`, materiais HQ OFF, reflexos/HDR OFF
- Sombras dinâmicas OFF; `MaxShadowResolution=1`
- Partículas / skeletal / draw distance no mínimo
- Pós OFF (motion blur, DoF, AO, bloom, shafts, flares, fog, distortion…)
- Apex / SpeedTree / folhagem / tessellation mínimos
- Menu sync: High Performance / efeitos OFF / Uncapped / escala 100%

---

## 4. CRIADOR — visual pra câmera + FPS

- Mantém texturas / materiais / reflexos mais altos no template principal
- Corta o que pouco aparece: MSAA/TAA/MLAA, Apex cloth/destruição, SpeedTree, foliage radius, tessellation pesada, blur samples=2
- FPS uncapped + VSync off (igual ao COMPLETO no pacing)
- Efeitos pesados reforçados nas seções filhas `SystemSettings*`

---

## 5. Launch options

Comando recomendado (botão **Copiar comando**):

```
-nomovie -NOSPLASH -nomansky +mat_antialias 0 -high
```

| Flag | Efeito |
|------|--------|
| `-nomovie` | Pula intros |
| `-NOSPLASH` | Pula splash |
| `-nomansky` | Céu mais leve |
| `+mat_antialias 0` | AA via console/mat |
| `-high` | Prioridade Alta — tire se stutter/áudio |

Alternativa sem prioridade: remova `-high`.

**Steam:** Propriedades → Opções de inicialização → colar.  
**Epic:** Gerenciar → Argumentos de linha de comando → colar.

---

## 6. Recuperação (resumo)

| Ação | Quando |
|------|--------|
| Corrigir permissões | INI bloqueado / ACL |
| Reparar perfil | Modo ativo mas INI/save driftou — reclampa + watcher |
| Recuperar boot | Jogo não abre — stock + limpa modo Gutty |
| Corrigir save | Menu vídeo errado / LOAD FAILURE |
| EAC 30005 | Serviço Easy Anti-Cheat preso |
| Corrigir Tudo | Pipeline completo de recuperação |
| Remover | Stock limpo + limpa watcher/tag |

---

## 7. O que a v25 NÃO faz

Diferente do `legacy/RL_GUTTYTECH_v21.5.bat`:

- Sem HPET / Dynamic Tick  
- Sem throttle de rede / Nagle / TCP  
- Sem prioridade de fila GPU no registro  

Só config do jogo + saves + watcher. Exige admin pela estabilidade do watcher/ACL, não para Ring-0.

---

## Segurança e rollback

- Backup automático; Remover / recuperação  
- Sem patch de `RocketLeague.exe`  
- Compatível com Easy Anti-Cheat  
- Não use “Play without EAC” em ranqueada  

---

*GUTTYTECH — RL Optimizer v25.0.5*
