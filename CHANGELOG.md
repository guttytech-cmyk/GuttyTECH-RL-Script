# Changelog

Todas as versões do **GUTTYTECH — Rocket League INI Optimizer** (TESSERACT).
Formato baseado em *Keep a Changelog*; datas em UTC.

## [v22.3.37] — 2026-07-22
### Alterado
- **CRIADOR:** Dynamic Shadows deixa de ser forçado OFF — pode ligar em Opções → Vídeo.
- Qualidade de sombra restaurada no perfil CRIADOR (resolução/texels stock) para o toggle do menu funcionar de verdade.
- `LightEnvironmentShadows=True` no template; preferência de `DynamicShadows` preservada ao reaplicar.

## [v22.3.36] — 2026-07-21
### Corrigido
- **Menu Epic COMPLETO (regressão v22.3.35):** `Value` como NameProperty fazia o jogo **ignorar** VideoOptions → Alta qualidade, 60 FPS, raios/clima ligados, partícula em branco.
- `Value` volta a **StrProperty**; `Id` continua NameProperty.
- VideoOptions **substituídos por completo** (não mescla Custom):
  - RenderQuality / RenderDetail / WorldDetail / ParticleDetail = `Performance` (UI PT: Desempenho)
  - TextureDetail = `TexturesLow` (UI: Alto desempenho / High Performance)
  - AntiAlias = 0
- Força sempre: MaxFPS=10000, bUncappedFramerate=True, bShowLightShafts=False, bShowWeatherFX=False, EffectIntensity=EI_Low.
- CompletoForce: UncappedFramerate=True, CustomFPS=0, MobileHeightFog=False.

## [v22.3.35] — 2026-07-21
### Corrigido
- **Menu Epic COMPLETO:** bug no patch `.save` — `for opt in COMPLETO_OPTIONS` desempacotava dict como `("Id","Value")` e gravava lixo; Render Quality ficava em High Quality.
- Menu agora sincroniza **High Performance** em Render Quality / Texture / World / Particle (e Render Detail = Performance).
- **Borda preta:** `CompletoForce` trava `ScreenPercentage=100`, `UpscaleScreenPercentage=True`, `MinimumScreenScale=100` (não preserva mais escala antiga).

### Alterado
- `VideoOptions.Value` serializado como `NameProperty` (FName), igual ao `Id`.

## [v22.3.34] — 2026-07-16
### Alterado
- **CORRIGIR ERROS** virou hub: Permissões | Recuperar boot | Tudo (submenu).
- **RESTAURAR PRESETS** no menu [6] — copia backup mais recente direto para SaveDataEpic.
- `RECUPERAR` CLI redireciona para `CORRIGIR-BOOT` (dentro de CORRIGIR ERROS).
- REMOVER continua só INI (preserva garagem).

## [v22.3.33] — 2026-07-16
### Corrigido
- **Menu Epic regressou:** clima ligado, FPS 60, raios de luz — CRIADOR tinha `MobileFog=True` no template.
- **VideoLockedKeys:** clima/raios/FPS nunca mais preservados do INI corrompido pelo jogo.
- **Patch .save reativado** (só flags de video/FPS; CRIADOR não mexe em qualidade de render).
- Sem apagar save, sem purgar RLSettingsData — presets do carro preservados.

## [v22.3.32] — 2026-07-16
### Corrigido
- **Presets do carro sumindo:** COMPLETO/CRIADOR agora mexem **só no INI** — não tocam no save Epic.
- **REMOVER** restaura apenas o INI (preserva garagem/presets). Save só no modo `RECUPERAR` (boot travado).
- Novo comando `RESTAURAR-PRESETS` — recupera save do backup mais recente em `%USERPROFILE%\GuttyTECH\RL-Optimizer-v22\Backups\`.

## [v22.3.31] — 2026-07-16
### Corrigido
- **Epic Online Services desconectado ao aplicar COMPLETO:** COMPLETO não apaga mais `RLSettingsData` (cache EOS). Purga permanece só no REMOVER.
- Passo renomeado para "Backup save Epic" — só backup de segurança, sem tocar na sessão online.

## [v22.3.30] — 2026-07-16
### Corrigido
- **Boot travado / jogo fecha:** REMOVER agora restaura **save Epic** + purga `RLSettingsData` (antes só INI).
- Script funciona **sem** `TASystemSettings.ini` (apagado).
- **Patch `.save` desativado** no COMPLETO — era a causa do jogo não abrir.
- Saves corrompidos vão para quarentena se não houver backup.

## [v22.3.29] — 2026-07-16
### Revertido
- **FramePacingForce removido** (v22.3.27) — impedia o RL de abrir.
- Frame pacing agora vem só do template em `[SystemSettings]` (sem force em perfis derivados).
- `WaitForGPU=False` (padrão stock) — `True` da v22.3.26+ travava boot em alguns PCs.

## [v22.3.28] — 2026-07-16
### Corrigido
- **Boot travado:** `OnlyStreamInTextures=False` (COMPLETO + CRIADOR).
- **FramePacingForce** restrito a `[SystemSettings]` principal — não espalha mais em perfis derivados.
- Anti-tela-preta mantém só `WaitForGPU` / `OneFrameThreadLag` / `AllowPerFrame*`.

## [v22.3.27] — 2026-07-16
### Corrigido
- **Tela preta em partida:** `FramePacingForce` trava frame pacing em todas as seções `[SystemSettings*]`.
  - Proibido: `WaitForGPU=False`, `OneFrameThreadLag=False`, `AllowPerFrameSleep/Yield=False`.
  - Forçado: `WaitForGPU=True`, `OneFrameThreadLag=True`, `AllowPerFrameSleep/Yield=True`.
- CRIADOR não preserva mais chaves de frame pacing corrompidas pelo jogo ao re-aplicar.

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
