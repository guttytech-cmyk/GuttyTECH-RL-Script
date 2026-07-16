# Changelog

Todas as versões do **GUTTYTECH — Rocket League INI Optimizer** (TESSERACT).
Formato baseado em *Keep a Changelog*; datas em UTC.

## [v22.3.26] — 2026-07-16
### Alterado
- `WaitForGPU=True` em COMPLETO e CRIADOR — CPU sincroniza com a GPU (frame pacing mais estável).

## [v22.3.25] — 2026-07-16
### Alterado
- `WaitForGPU=False` (padrao stock) em COMPLETO e CRIADOR.

## [v22.3.24] — 2026-07-16
### Corrigido
- **COMPLETO e CRIADOR:** baixar resolução ou qualidade de renderização (escala 3D) no menu sem tela preta / janela minúscula.
  - `UpscaleScreenPercentage=True` no COMPLETO; `CompletoForce` não força mais upscale off.
  - COMPLETO deixa o INI gravável e preserva `ScreenPercentage` / upscale ao re-aplicar.
  - PC fraco pode usar menu Vídeo para render quality + resolução em ambos os modos.

## [v22.3.23] — 2026-07-16
### Revertido
- Hotfixes v22.3.21/v22.3.22 de loading infinito — problema persistiu no PC do cliente mesmo após REMOVER.
- Script restaurado ao estado da **v22.3.20** (templates + `CompletoForce`).

## [v22.3.22] — 2026-07-16
### Corrigido
- **Loading infinito / "Não está respondendo":** `OnlyStreamInTextures=False` restaurado (CRIADOR e COMPLETO).
  - Regressão introduzida no CRIADOR entre v22.3.1 e v22.3.20; streaming de texturas travava boot DX11 em alguns PCs.
- Revertido hotfix v22.3.21 (`CompletoForce`/`WaitForGPU`) — não resolveu o caso reportado.

## [v22.3.21] — 2026-07-16
### Corrigido
- **COMPLETO:** tentativa de fix loading via `WaitForGPU` + `CompletoForce` restrito (revertido em v22.3.22).

## [v22.3.20] — 2026-07-15
### Alterado
- **Runtime embutido:** Python 3.11 + nixwrap + patch `.save` dentro do `GuttyTECH_RL.exe`.
  - Cliente baixa **apenas o exe** (~80 MB); extrai 1× em `%USERPROFILE%\GuttyTECH\RL-Optimizer-v22\runtime\`.
  - Sem `tools.zip`, sem Python instalado no PC.

## [v22.3.19] — 2026-07-15
### Corrigido
- **COMPLETO:** indicador circular branco sob a bola (gameplay) restaurado.
  - `DynamicDecals=True` e `DecalCullDistanceScale=1.0` no INI principal.
  - Save patch: `WorldDetail=Quality`, `bUseBallIndicator=True`.
- **COMPLETO:** menu de vídeo Epic sincronizado via patch do `.save` (sem tutorial).
  - `EffectIntensity=EI_Low`, textura/partículas/mundo no potato, FPS uncapped (`MaxFPS=10000`).
  - Purga apenas `RLSettingsData` (preserva progresso).
- **CRIADOR:** grama SpeedTree (`Leaves`/`Fronds`) restaurada no template.
- **Save codec:** enums `ByteProperty` corrigidos no `nixwrap` (`save_codec.py`).

### Adicionado
- `CompletoForce`, `CriadorForce`, `VideoSettingsSync`, `IniAudit` (AUDIT).
- Build `embed-bundle.zip` (Python embed + nixwrap) embutido no exe.

## [v22.3] — 2026-06-23
### Corrigido
- **CRIADOR:** indicador da bola (círculo branco + sombra no chão) voltou a aparecer nas laterais/cantos do campo.
  - `UnbatchedDecals=True` e `bEnableForegroundShadowsOnWorld=True` no template CRIADOR.
  - Sombras dinâmicas continuam OFF (sem custo de FPS das dynamic shadows).
### Alterado
- Executável renomeado para **`GuttyTECH_RL.exe`** (nome oficial do release no GitHub).

## [v22.2] — 2026-06-22
### Adicionado
- **Launch Options helper** (Menu → `[4]`): telas dedicadas para **Steam** e **Epic**, com passo a passo de onde colar.
  - **Clipboard automático** via `clip.exe` (sem dependências), com fallback manual caso a cópia falhe.
  - Comando padrão (Steam e Epic, idênticos): `-nomovie -NOSPLASH -high`.
  - Pesquisa validada por **swarm de 3 agentes** (Steam / Epic / placebo-safety) + buscas próprias.
  - **Tela honesta:** explica que launch options quase não mudam FPS no RL — o ganho real é o INI + Opções > Vídeo.
### Removido do comando (placebo/no-op confirmado no RL/UE3)
- `-NoVSync` (o INI já desliga o V-Sync), `-nolog`, `-NoSteamVR` (o RL não tem plugin de VR),
  `-no-stereo-rendering`, `-USEALLAVAILABLECORES` (no-op na engine do RL), `-malloc=system`.
### Opcional
- `-NoForceFeedback` (colar à mão; só se você não quer vibração no controle).
### Segurança
- Todas as flags validadas contra o **Easy Anti-Cheat (EAC)**, obrigatório no online desde a Season 22.
- Nenhuma flag arriscada/banível no comando padrão.

## [v22.1] — 2026-06-22
### Otimizado
- 6 chaves adicionais no modo **COMPLETO**:
  - `MaxDrawDistanceScale`: 1 → 0
  - `OnlyStreamInTextures`: False → True
  - `MotionBlurSkinning`: 1 → 0
  - `SkeletalMeshLODBias`: 15 → 100
  - `ParticleLODBias`: 15 → 100
  - `DecalCullDistanceScale`: 1.0 → 0.0
- **CRIADOR** intacto (nenhuma mudança).

## [v22.0] — 2026-06-22
### Adicionado
- Aplicativo **.NET 9 single-file** (~10,5 MB), autossuficiente, sem dependências.
- 3 modos: **COMPLETO**, **CRIADOR**, **REMOVER**.
- **UI Awwwards:** banner gradiente, spinner Braille, painel CONCLUÍDO com glow/gradiente, checklist, botão ENTER.
- **Unlock robusto:** deleta e recria o arquivo travado (resolve a maioria dos casos sem admin; elevação só sob demanda).
- **Preservação de resolução / modo de tela** ao aplicar qualquer modo (sem tela preta).
- Detecção de INI legado da v21 (avisa para reaplicar pela v22).
- Build automático via `build_exe.bat`.
### Corrigido (vs v21.x)
- Regex do PowerShell quebrando blocos `TEXTUREGROUP`.
- Variável inconsistente (`RL_TARGET` usada sem ser definida) que fazia o script não aplicar nada.
- `attrib +r` + resolução forçada travando o jogo / tela preta.
- `BCDEDIT` e tweaks de TCP removidos (eram system-wide e arriscados).
- Resolução forçada 1920x1080 quebrando monitores não-1080p.
- Backups iam para `%TEMP%` (volátil) → agora persistentes em `%USERPROFILE%\GuttyTECH`.
