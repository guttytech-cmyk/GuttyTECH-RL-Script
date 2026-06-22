# GUTTYTECH — Rocket League INI Optimizer v22.2

> **O que é:** um `.exe` único (~10,5 MB) que otimiza o `TASystemSettings.ini` do Rocket League (Unreal Engine 3).  
> **O que NÃO é:** não mexe no Windows, registro, rede, HPET nem TCP. Isso ficou no script antigo em [`legacy/RL_GUTTYTECH_v21.5.bat`](legacy/RL_GUTTYTECH_v21.5.bat).

**Menu do app**

| Tecla | Função |
|:---:|--------|
| `[1]` | **COMPLETO** — FPS máximo, visual mínimo (competitivo / PC fraco) |
| `[2]` | **CRIADOR** — otimizado sem destruir o visual (streamers / YouTubers) |
| `[3]` | **REMOVER** — restaura o original / padrão e destrava o arquivo |
| `[4]` | **LAUNCH OPTIONS** — copia o comando de inicialização (Steam/Epic) pro clipboard |
| `[5]` | Sair |

---

## 1. O APP EM SI (o que o código faz de verdade)

- **Acha o arquivo sozinho** — procura `TASystemSettings.ini` em `Documents`, `OneDrive`, `OneDrive - Personal`, `OneDrive - Pessoal` e varre perfis de usuário no PC (pastas com acento, ex.: `Usuário`, `João`).
- **Impede aplicar com o jogo aberto** — se o Rocket League estiver rodando, avisa (o jogo sobrescreve o `.ini` ao fechar).
- **Backup antes de tudo** — salva cópias em `%USERPROFILE%\GuttyTECH\RL-Optimizer-v22\Backups\` (com data/hora). Na 1ª vez, guarda também o seu original em `TASystemSettings.original.ini`.
- **Destrava arquivo travado** — remove read-only, roda `takeown` + `icacls /reset` + concede permissão ao seu usuário. Resolve scripts antigos que bloquearam o acesso. **Não exige admin** na maioria dos casos (só eleva se ainda falhar).
- **Apaga e recria o `.ini`** — em vez de editar linha a linha (que quebrava no v21), substitui o arquivo inteiro pelo template do modo escolhido.
- **Preserva sua resolução e modo de tela** — `ResX`, `ResY`, `Fullscreen`, `Borderless` e `AutoDetectDesktopResolution` do seu arquivo atual são mantidos. Nada de forçar 1920×1080 nem tela preta.
- **Trava no final (somente-leitura)** — impede Steam/Epic de apagarem o tweak na próxima abertura. O modo **REMOVER** destrava de volta.
- **Detecta INI legado v21** — se encontrar fingerprint antigo (`MaxLODSize=16` sem sentinel v22), avisa para reaplicar.
- **UI animada** — banner gradiente, spinner Braille nos passos, painel CONCLUÍDO com checklist do que ajustar 1× em Opções > Vídeo.

---

## 2. SINCRONIA E FLUIDEZ (nos dois modos COMPLETO e CRIADOR)

| Config | Valor | O que significa na prática |
|--------|:-----:|----------------------------|
| `UncappedFramerate` | **True** | Tira o teto de FPS do engine — sua GPU pode correr solta |
| `bSmoothFrameRate` | **False** | Para de “suavizar” o FPS em média — menos cap artificial |
| `UseVsync` | **False** | V-Sync desligado no `.ini` (menos input lag vs sync vertical) |
| `WaitForGPU` | **True** | CPU espera a GPU terminar o frame — mais estável, menos tearing interno |
| `OneFrameThreadLag` | **True** | Engine fica 1 frame atrás na thread de render — trade-off estabilidade vs latência mínima |
| `AllowPerFrameSleep` | **True** | Permite micro-pausas entre frames (engine UE3 padrão) |
| `AllowPerFrameYield` | **True** | Engine cede tempo pra outras threads — evita travamento geral |
| `CustomFPS` | **0** (COMPLETO) | Sem FPS custom fixo no `.ini` |

> **Importante:** o ganho de FPS vem sobretudo das texturas/efeitos/sombras abaixo + **Opções > Vídeo** in-game. Launch options quase não mudam FPS (veja seção 9).

---

## 3. MODO COMPLETO — texturas e qualidade visual

Visual “batata” de propósito — tudo no menor custo possível:

- **Texturas 16×16 em todos os grupos** (`MaxLODSize=16`, `MinLODSize=1`) — carro, chão, estádio, UI, tudo no mínimo
- **LOD bias máximo** (`LODBias=15`) — sempre o modelo mais simples/longe
- **Filtro Point** (`MinMagFilter=Point`, `MipFilter=Point`) — sem suavização, pixelado, GPU trabalha menos
- **Anisotrópico zerado** (`MaxAnisotropy=0`) — chão e paredes de longe perdem nitidez
- **DetailMode=0** — versões de baixo detalhe dos objetos
- **Materiais HQ desligados** (`bAllowHighQualityMaterials=False`) — metal/cromo viram plástico simples
- **v22.1 extras:**
  - `MaxDrawDistanceScale=0` — quase nada é desenhado longe
  - `OnlyStreamInTextures=True` — só carrega textura quando precisa (menos VRAM)
  - `SkeletalMeshLODBias=100` / `ParticleLODBias=100` — carros e partículas no LOD mais feio
  - `MotionBlurSkinning=0` — sem blur em animação de mesh
  - `DecalCullDistanceScale=0` — decals de distância zerados (marcas de chão somem mais cedo)

---

## 4. MODO COMPLETO — física, decals e destruição

- **Tesselação zerada** (`TessellationAdaptivePixelsPerTriangle=0`) — superfícies em blocos, sem subdividir triângulos
- **Reflexos de imagem OFF** (`AllowImageReflections=False`, `AllowImageReflectionShadowing=False`) — carro não espelha o estádio
- **HDR OFF** (`FloatingPointRenderTargets=False`) — luz 8-bit simples, sem cálculo HDR
- **Apex Cloth OFF** (`AllowApexCloth=False`) — bandeiras, antenas e tecidos param de balançar
- **Destruição/fratura OFF** — `bAllowFracturedDamage=False`, escalas de spawn/fratura zeradas — sem cacos na demo
- **Subsurface scattering OFF** — sem luz “passando por dentro” de materiais
- **Translucência separada OFF** — vidro/gel não em camada extra
- **MLAA OFF** — sem anti-serrilhado por software
- **SpeedTree OFF** — sem folhas/galhos com física
- **Decals:**
  - `StaticDecals=False` — linhas/logos estáticos do chão OFF
  - `DynamicDecals=True` — **mantém** marcas dinâmicas (rastro de pneu ao jogar)
  - `UnbatchedDecals=False`

---

## 5. MODO COMPLETO — pós-processamento

Tudo desligado para economizar GPU:

| Efeito | Status |
|--------|:------:|
| Motion Blur / Motion Blur Pause | OFF |
| Depth of Field (desfoque de profundidade) | OFF |
| Ambient Occlusion (oclusão de ambiente) | OFF |
| Bloom | OFF |
| Light Shafts (raios de luz) | OFF |
| Lens Flares | OFF |
| Fog Volumes (névoa) | OFF |
| Distortion / Filtered Distortion / Particle Distortion | OFF |
| Radial Blur | OFF |

---

## 6. MODO COMPLETO — sombras

- **Sombras dinâmicas OFF** em todos os perfis (`DynamicShadows=False`)
- **Sombras de ambiente / compostas / foreground / dominantes** — OFF
- **Resolução de sombra destruída** — `MinShadowResolution=16`, `MaxShadowResolution=16`, `MaxWholeSceneDominantShadowResolution=16`
- **Penumbra cascata zerada** (`CSMSplitPenumbraScale=0`)
- **Texels por pixel de sombra = 0** (`ShadowTexelsPerPixel=0`)

---

## 7. MODO COMPLETO — renderização

- **Tela em 100%** (`ScreenPercentage=100`) — renderiza no tamanho real do monitor (sua resolução é preservada pelo app)
- **Upscale ativo** (`UpscaleScreenPercentage=True`) — estica se resolução interna for menor
- **Escala mínima travada** (`MinimumScreenScale=1.0`) — não cria janela minúscula
- **MSAA / Temporal AA OFF**
- **Multisamples = 0**

---

## 8. MODO CRIADOR — o que é diferente (visual preservado)

Para streamers/criadores — bonito na câmera, mas mais leve que o stock:

| Item | CRIADOR | COMPLETO |
|------|:-------:|:--------:|
| Texturas mundo/carro | até **1024px**, filtro **Anisotrópico** | **16px**, filtro **Point** |
| `MaxAnisotropy` | **16** | **0** |
| `DetailMode` | **2** (alto) | **0** (mínimo) |
| Materiais HQ | **ON** | OFF |
| Reflexos no carro | **ON** | OFF |
| HDR (`FloatingPointRenderTargets`) | **ON** | OFF |
| Apex Cloth (bandeiras/capas) | **ON** | OFF |
| SpeedTree (folhas) | **ON** | OFF |
| Fog volumes | **ON** | OFF |
| Decals estáticos | **ON** | OFF |
| Decals dinâmicos (pneu) | **ON** | ON |
| `MaxDrawDistanceScale` | **1** (normal) | **0** |
| `OnlyStreamInTextures` | **False** | **True** |
| Sombras dinâmicas | **OFF** (ganho de FPS) | OFF |
| Motion blur / DoF / Bloom / AO / distorção | **OFF** | OFF |
| `UncappedFramerate` + `bSmoothFrameRate=False` + `UseVsync=False` | **igual** | **igual** |

**Ajuste in-game 1× (CRIADOR):** Render = Alta Qualidade, Textura = Alta Qualidade, Sombras Dinâmicas = OFF, Motion Blur/DoF/Bloom = OFF, V-Sync = OFF.

**Ajuste in-game 1× (COMPLETO):** Render = Performance, Textura = Performance, Anti-Alias = OFF, V-Sync = OFF, Efeitos = OFF.

---

## 9. BÔNUS — LAUNCH OPTIONS (Steam / Epic)

No menu `[4]`, o app **copia automaticamente** pro clipboard:

```
-nomovie -NOSPLASH -high
```

| Código | Status no RL | O que faz |
|--------|:------------:|-----------|
| `-nomovie` | ✅ Real | Pula vídeos de intro — boot mais rápido |
| `-NOSPLASH` | ✅ Real | Pula splash screen — boot mais rápido |
| `-high` | ✅ Real | Prioridade **Alta** no Windows — tire se der stutter/estalo de áudio |
| `-NoVSync` | ❌ Placebo | O RL ignora; V-Sync já vai OFF pelo `.ini` |
| `-nolog` | ❌ Placebo | Sem efeito útil; ganho zero |
| `-NoForceFeedback` | ⚠️ Opcional | Só se não quiser vibração no controle (cole à mão) |
| `-no-stereo-rendering` | ❌ Placebo | RL não renderiza estéreo |
| `-NoSteamVR` / `-USEALLAVAILABLECORES` / `-malloc=system` | ❌ No-op | Ignorados pela engine do RL |

> Launch options quase **não mudam FPS**. O ganho real é o `.ini` + Opções > Vídeo. Tudo **seguro com Easy Anti-Cheat** (EAC).

### Como colocar na Steam

1. Steam → botão direito em **Rocket League** → **Propriedades**
2. **Geral** → **Opções de inicialização**
3. Cole: `-nomovie -NOSPLASH -high`
4. Feche e abra o jogo

### Como colocar na Epic Games

1. Epic Launcher → **Biblioteca** → **⋯** no Rocket League → **Gerenciar**
2. Ative **Argumentos de linha de comando adicionais**
3. Cole: `-nomovie -NOSPLASH -high`
4. Salve e abra o jogo

---

## 10. O QUE O v22.2 NÃO FAZ (diferente do v21)

O script antigo (`legacy/RL_GUTTYTECH_v21.5.bat`) também mexia em:

- Timer HPET / Dynamic Tick do Windows  
- Network throttling / Nagle / TCP  
- Prioridade de fila GPU no registro  
- Modo econômico do processador  

**O v22.2 não faz nada disso.** Só o `TASystemSettings.ini`. Mais seguro, roda sem admin, não quebra Secure Boot/BitLocker, e funciona em qualquer PC onde o v21 falhava.

---

## Segurança e rollback

- Backup automático antes de cada alteração  
- **REMOVER** restaura original ou stock (mantendo resolução)  
- Não toca em arquivos do sistema  
- Compatível com **Easy Anti-Cheat** (online obrigatório desde Season 22)  
- Não use “Play without Easy Anti-Cheat” para ranqueada — só treino offline/LAN  

---

*GUTTYTECH — TESSERACT v22.2*  
*"Você vai otimizar o jogo ou vai continuar sofrendo por culpa da engine burra do jogo?"*
