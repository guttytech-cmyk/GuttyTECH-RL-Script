# Changelog

Todas as versões do **GUTTYTECH — Rocket League Optimizer**.
Formato baseado em *Keep a Changelog*; datas em UTC.

## [v25.0.31] — 2026-11-08
### Corrigido
- COMPLETO e CRIADOR só gravam o `TASystemSettings.ini`. O app não reescreve mais o `.save`.
- O watcher só vigia o INI. Ele não copia nem restaura save quando o jogo abre ou fecha.
- Na abertura, os processos antigos do Gutty e o Python da pasta do otimizador são encerrados.
- RESTAURAR PRESETS esvazia o save local. O Rocket League cria um arquivo novo e os itens continuam na conta. Os backups das versões 23 a 30 vão para `Deprecated_Pre25_0_31` e não voltam para o jogo.
- Na primeira abertura desta versão, essa limpeza roda uma vez sozinha.

## [v25.0.30] — 2026-11-05
### Corrigido
- O conserto da cor era desfeito na hora: o detector ainda marcava o save reparado como quebrado e o app copiava outro arquivo por cima.
- RESTAURAR PRESETS repara a cor e o id do carro nos backups (inclusive na quarentena) antes de escolher a garagem.
- O watcher não cobre mais uma garagem grande no fecho do jogo. Só recupera save que encolheu para stub.
- Pasta de save vazia não conta mais como menu revertido.
- COMPLETO mantém o buffer de cor e o material da pintura, para o laranja não mudar dentro da partida.
- O patch do save não muda a data do arquivo, para o jogo não passar a abrir o slot velho.
- O vínculo do preset (ObjectProperty) volta para o formato do jogo. Era isso que deixava o Fennec cair no Octane.
- Um teto de FPS escrito no INI (144, 240, 360) vale mesmo se Uncapped ainda estiver True. O template, com CustomFPS=0, continua ilimitado.
- Se o jogador apagou um preset, o backup antigo não cobre o save mais novo.

## [v25.0.29] — 2026-11-05
### Corrigido
- A cor e o Octane travado estão no save, não no detalhe do INI. A versão antiga gravou a cor como número inteiro e cortou o id do item. O jogo não lê isso, ignora o Fennec e mostra outro Octane. O app agora regrava só esses campos no formato original, com o jogo fechado.

## [v25.0.28] — 2026-11-05
### Corrigido
- O Octane feio continuava depois de soltar a malha e a textura do carro. O COMPLETO ainda gravava DetailMode=0, e o jogo esconde o Fennec. Agora o detalhe do carro fica em 2. O mapa continua baixo.

## [v25.0.27] — 2026-11-05
### Corrigido
- Recuperar o boot esvaziava a pasta do save e o jogo criava um perfil novo, sem a cor. Agora a cópia íntegra volta para a pasta antes de abrir o jogo. Save regravado continua de fora.
- A busca da cópia boa não enche mais o log com um aviso por arquivo.

## [v25.0.26] — 2026-11-05
### Corrigido
- O limite de FPS escolhido no menu (120 a 960, inclusive 360) fica. O modo continua ilimitado quando o jogo volta para 60.
- O carro não vira mais o Octane padrão. A malha não é mais empurrada para o último LOD, e a textura do carro fica em 1024.
- A tabela de cor não vai mais para 2×2. Laranja escolhido deixava de ser laranja.
- Save regravado pela v25.0.22 perde a tinta. Com o jogo fechado, abrir o app ou REMOVER troca esse arquivo pela cópia íntegra.
- Com o jogo aberto, se o Rocket League reescrever textura, partícula ou shader, o app repõe o INI na hora, sem travar o arquivo.
- Atualizar o app não deixa o watcher da versão antiga vivo. Esse processo regravava o save e o reforço desfazia o menu.
### Melhorado
- O diagnóstico não diz mais que o menu foi adiado em conta que só tem garagem.

## [v25.0.25] — 2026-11-04
### Corrigido
- Causa raiz de "não aplica", preset sumindo e cor do carro mudando: o codec antigo regravava o save com o tamanho dos campos errado (bool e enum). O jogo descartava o bloco inteiro e voltava para o padrão — menu em Alta qualidade/60 FPS, preset solto e tinta na cor do time.
- Novo patch cirúrgico do `.save`: só os blocos de vídeo, gameplay e câmera mudam, no formato nativo do jogo. Garagem, presets, tinta e inventário ficam byte a byte iguais (conferido antes e depois de gravar). Vale para contas com garagem e para o save Steam, que antes nunca sincronizava.
- Saves leves já regravados por versões antigas são consertados no próximo apply.
- REMOVER agora também devolve o menu de vídeo do save ao original. Antes só o INI voltava e o jogo reaplicava o modo no boot seguinte.
- RESTAURAR PRESETS repõe também os slots `_1`/`_2`/`_3` quebrados, grava com a data de agora (o cloud não passa por cima), não cobre um save íntegro e atual com backup velho e escolhe a cópia com mais presets. Conta Epic vai só para a pasta Epic, Steam só para a Steam.
- Depois de restaurar, o menu de vídeo é alinhado ao modo ativo.
### Melhorado
- Backups só são criados quando o save mudou, e o histórico por conta é podado (8 mais recentes + 2 maiores). Antes cada fecho do jogo duplicava todos os saves e a pasta chegava a vários GB.

## [v25.0.24] — 2026-11-03
### Corrigido
- O sync de vídeo não regrava mais o save da garagem. A regravação apagava o vínculo do preset e a tinta, e na partida o carro entrava com a cor do time.
- RESTAURAR PRESETS ignora save regravado e devolve a cópia íntegra, mesmo menor. O cofre Best não guarda mais o arquivo quebrado.
- O reforço automático não cobre uma garagem saudável só porque o backup é maior, e não recria slot que o jogo apagou.
- O diagnóstico avisa quando o save foi regravado.

## [v25.0.23] — 2026-11-02
### Melhorado
- Correções e melhorias na interface: tudo numa página só, sem abas e sem submenu. Status, modos, comando de inicialização, protocolo, recuperação, suporte e o remover ficam visíveis.
- O comando da Steam/Epic fica logo abaixo dos modos. O botão de remover a otimização ficou grande.
- Scroll mais suave e entrada da tela mais calma.
### Corrigido
- O watcher do modo CRIADOR agora detecta e corrige drift, do mesmo jeito que o COMPLETO.
- Remover a otimização também apaga a marca da versão do jogo, para o próximo apply não achar um patch fantasma.
- Troca de idioma e o download da atualização não travam a interface.

## [v25.0.22] — 2026-09-26
### Melhorado
- Se o Rocket League atualizar (exe novo), o watcher reaplica COMPLETO/CRIADOR sozinho. Sem aviso pra clicar de novo.
- Com o jogo aberto, o processo em segundo plano conta chaves fora do contrato. No fecho, se forem 3 ou mais, reescreve o template inteiro.
- Save de garagem (acima de 1,2 MB) entra no sync de vídeo, com rollback se o arquivo encolher. Era isso que fazia o menu desfazer a textura no boot seguinte.

## [v25.0.21] — 2026-09-23
### Adicionado
- Botão **PT / EN** no canto superior direito. O padrão continua português.
- A interface troca na hora: menus, modos, recuperação, sistema, avisos e o overlay de operação.

## [v25.0.20] — 2026-08-22
### Corrigido
- **MENĐONÇA / OneDrive:** se existia um perfil Windows antigo (`Gustavo`) com Documentos local, o Gutty pinava esse INI e o restore gravava presets no perfil errado. O jogo (conta `gusta`) continuava no OneDrive.
- Agora ignora pin/pasta de outro usuario, nao varre outros perfis se o seu ja tem RL (mesmo no OneDrive), e copia **a pasta do seu usuario** para Documentos local.
- Nao desfaz o redirecionamento de Documentos se a unica pasta encontrada for de outro usuario Windows (PC compartilhado / perfil antigo).

## [v25.0.19] — 2026-08-20
### Melhorado
- **Changelog:** cada versão vira um card (chip NOVA na mais recente). O X de fechar ficou visível — antes o padding recortava o glifo.

## [v25.0.18] — 2026-08-20
### Melhorado
- **Changelog:** quem pula versões (ex.: 25.0.10 → 25.0.18) vê a lista de **todas** as versões no meio, não só a última. O popup de atualizar e a 1ª abertura da versão nova usam essa lista.
- Popup de versão nova grava o texto no code-behind (sem binding TwoWay no TextBox) — o crash do `Notes` não volta.

## [v25.0.17] — 2026-08-20
### Corrigido
- Popup de versão nova crashava com “associação TwoWay… propriedade somente leitura Notes”. O TextBox do changelog agora liga em OneWay.

## [v25.0.16] — 2026-08-20
### Melhorado
- **ATUALIZAR:** o botão muda para **BUSCANDO…**, pulsa e mostra aviso enquanto consulta o GitHub — deixa de parecer travado. O clique também deixa de ser ignorado se o status de fundo ainda estiver atualizando.

## [v25.0.15] — 2026-08-19
### Corrigido
- Fechar a janela disparava um diálogo falso de “DLL nativa faltando” (`Dll was not found` no unload da CRT/WPF). O app já tinha saído; agora ignora esse ruído.

## [v25.0.14] — 2026-08-17
### Melhorado
- **Detecção da pasta do RL:** procura Epic manifests, LauncherInstalled, Steam `libraryfolders`, registro de desinstalação e a pasta do próprio exe (instalação custom).
- ZIP de suporte ganha `game-install.txt` (veredito OK / INCOMPLETO / NAO_INSTALADO + todos os caminhos testados).
- Diagnóstico deixa de sugerir Corrigir Save / Recuperar Boot quando o jogo não está instalado.
- Recuperar Boot / Corrigir Tudo / Reparar EAC deixam de reportar sucesso se o setup do EAC não existir.

## [v25.0.13] — 2026-08-13
### Corrigido
- **OneDrive / presets online:** o Windows pode continuar a apontar Documentos para `OneDrive\Documentos` mesmo com o OneDrive desinstalado. O Gutty preferia esse caminho, recriava a pasta no restore e copiava saves Epic para Steam.
- Agora escolhe Documentos **local**, migra a pasta do RL para fora do OneDrive, desfaz o redirecionamento, e **nao cria** pasta Steam/OneDrive se nao existia.
- Presets OK offline e a falhar online: Epic Cloud + pasta OneDrive a lutar com o save local.

## [v25.0.12] — 2026-08-13
### Corrigido
- Popup CHANGELOG na UI: remove Markdown (`**`, crases, links) — texto limpo legível no WPF.

## [v25.0.11] — 2026-08-12
### Melhorado
- Diagnóstico alerta **OneDrive** (risco de preset com nome + Octane).
- Após **CORRIGIR TUDO / RECUPERAR BOOT**: aviso obrigatório de **RESTAURAR PRESETS**.

## [v25.0.10] — 2026-08-10
### Alterado
- Changelog na UI no **mesmo texto/estilo Discord** (`**O que mudou:**`, bullets em linguagem de pessoa; formatador não “tecniciza” mais as notas).

## [v25.0.9] — 2026-08-10
### Novo
- **Changelog na atualização** (estilo Discord): popup ao ATUALIZAR / BAIXAR e na 1ª abertura da versão nova, com as notas da release do GitHub.

## [v25.0.8] — 2026-08-10
### Corrigido
- Conta **só com save de garagem** (~2 MB): sync de menu deixa de falhar a operação (INI + watcher = sucesso; menu adiado).
- **INI inchado** no REPARAR/reclamp: reescreve do template limpo (mantém resolução/borda) + dedupe de chaves por seção.

### Melhorado
- ZIP de suporte: alerta de só-garagem, INI inchado/dups, versão do app em `system.txt`/`diagnostico`, índice sem ruído `watch-extract`/resources.

## [v25.0.7] — 2026-08-07
### Alterado
- `MaxFilterBlurSampleCount=1` no COMPLETO/CRIADOR (era 2) — **aprovado** após teste de boot/partida.
  - **0 continua proibido** (crash KERNELBASE).

## [v25.0.6] — 2026-08-06
### Corrigido
- **Sombra/indicador da bola nos cantos** voltou: `UnbatchedDecals=True` no COMPLETO e CRIADOR.
  - Regressão do potato extremo forçava `UnbatchedDecals=False` e apagava a sombra nas walls/corners.
  - Mantém `DynamicDecals=True`, `DecalCullDistanceScale=1.0`, `bEnableForegroundShadowsOnWorld=True`.
  - Watcher/drift passa a reclamar se o jogo desligar essas chaves.

## [v25.0.5] — 2026-08-04
### Corrigido / Melhorado
- Detecção COMPLETO/CRIADOR por marcador + fingerprint + tag local.
- Admin obrigatório via TokenElevation no arranque.
- Card **PROTEÇÃO** (watcher) — INI permanece gravável para o menu de vídeo.
- Recuperação: boot/Corrigir Tudo limpam modo; Reparar Perfil arranca watcher; CORRIGIR SAVE e diagnóstico honestos; EAC via manifests Epic.
- Remover força stock limpo se backup estiver poluído.
- Copiar comando funciona com app elevado (clipboard de-elevado + ficheiro no Desktop).
- ZIP de suporte completo para enviar no chat.

## [v25.0.1] — 2026-08-04
### Corrigido
- Reparar EAC 30005.
- Completo/Criador FPS + watcher drift.

## [v25.0.0] — 2026-08-04
### Alterado
- UI WPF com motion + startup robusto (substitui o menu console antigo).

## [v24.0.0] — 2026-07-25
### Corrigido / Melhorado — PRESETS / GARAGEM
- **Causa:** backups so guardavam saves leves (&lt;1.2MB = video). Presets do carro ficam em saves **grandes** (~2MB+) e nao eram copiados — [6] RESTAURAR PRESETS nao tinha o que restaurar.
- Agora o backup inclui **saves de garagem** (1.5–12MB) em todo Apply/sync + pasta `Backups\\Presets`.
- **RESTAURAR PRESETS** prioriza o save **maior** (garagem) por conta, procura tambem Quarentena, limpa `RLSettingsData`, e pede abrir o RL offline 1x (cloud Epic).
- Snapshot do live grande antes de restaurar (nao perde a unica copia).

## [v23.0.8] — 2026-07-25
### Corrigido
- Watcher so arranca no **menu interativo** (Apply/Reparar). No CLI evitava-se hang do `-Wait` UAC/PowerShell no processo-filho; auto-heal no proximo arranque cobre o CLI.

## [v23.0.7] — 2026-07-25
### Corrigido
- Watcher usa `DOTNET_BUNDLE_EXTRACT_BASE_DIR` proprio — o pai (Apply/REPARAR) ja nao fica preso no exit por causa do mutex do single-file.

## [v23.0.6] — 2026-07-25
### Corrigido (pos-monitor CRIADOR+COMPLETO)
- Watcher **desprende** do job do pai (`CreateProcess` + `BREAKAWAY_FROM_JOB`) — Apply/CORRIGIR-PERFIL ja nao ficam presos no `-Wait`.
- Save: flags omitidas pelo jogo (ex. `bShowLensFlares`) deixam de marcar falso BAD; so falha se estiverem `True` indevidamente.
- Steam `.save` corrompido/stub: ignorado sem falhar o heal Epic.

## [v23.0.5] — 2026-07-25
### Corrigido / Melhorado
- **Watcher unico:** ao reaplicar, mata o WATCH anterior (`watcher.lock` + PID) — sem processos duplicados.
- Watcher faz **2o pass** ~8s apos o heal (cloud Epic por vezes regrava).
- **CORRIGIR ERROS** refeito:
  - `[2] REPARAR PERFIL` — mantem COMPLETO/CRIADOR (reclamp INI + sync menu + cache)
  - `[3] RECUPERAR BOOT` — stock (ultimo recurso); pergunta se prefere reparar perfil
  - `[4] DIAGNOSTICO` — mostra INI/permissoes/modo/boot killers
  - `[5] TUDO` — permissoes + reparar (ou boot se sem modo)
- Save recovery tambem cobre **Steam** (`SaveData`), nao so Epic.

## [v23.0.4] — 2026-07-25
### Corrigido
- **Monitor ciclo:** ao **abrir** o RL apos COMPLETO, o jogo esvazia `VideoOptions` (MaxFPS=62) e reescreve INI (`UncappedFramerate=False`, shafts/shaders ON).
- Novo **watcher** (`WATCH`): depois de aplicar, fica a escuta — quando o RL fecha, reclampa INI (CompletoForce) + sync do menu sozinho.
- Auto-heal no arranque do app tambem reclampa o INI (nao so o save).

## [v23.0.3] — 2026-07-25
### Alterado
- **Revisao total:** CompletoForce reforçado (OnlyStreamInTextures/WaitForGPU/VSync/shadow res) para aguentar reescrita do APLICAR.
- CRIADOR: removido `MobileMinimizeFogShaders` duplicado (FALSE ganhava).
- IniAudit: boot killers + ScreenPercentage + duplicatas + Fullscreen/Borderless exclusivos.
- Backup de save alinhado com patch (6 ficheiros).

## [v23.0.2] — 2026-07-25
### Corrigido
- Auditoria: apos APLICAR Sem bordas o RL reescreveu o INI (`ParticleLODBias=1`, shaders ON) e deixou o save com `RenderDetail=Custom` — isso **nao** e o padrao do jogo; e estado hibrido que faz o menu ir ao maximo.
- Marcador duravel `GuttyTechMode=COMPLETO|CRIADOR` (o comentario `;GUTTYTECH...` o jogo apaga).
- Detect/auto-heal voltam a funcionar depois do APLICAR.

## [v23.0.1] — 2026-07-25
### Corrigido
- **Clicar APLICAR em Sem bordas/resolucao** faz o RL reescrever o menu para Alta qualidade / efeitos ON — comportamento do jogo, nao do INI.
- Preserve resolucao/borda **antes** do REMOVER interno (antes voltava ao Fullscreen do backup original).
- Patch detecta `RenderDetail=Custom` (pos-APLICAR) e regrava perfil COMPLETO; preserva `WindowMode`/`Resolution`.

## [v23.0.0] — 2026-07-24
### Alterado
- **Major v23:** sync de video com **barra de progresso** real (`[####----] 45% 3/6`).
- Sync so nos **6 saves mais recentes e &lt;1.2MB** (os de 2MB+ eram o “travamento”).
- Sem Prompt S/N no meio do sync (fechava o RL sozinho) — isso pedia Enter e parecia travado.
- Flush do teclado antes/depois do sync.

## [v22.3.48] — 2026-07-24
### Alterado
- **Sync de video mais rapido:** so regrava saves que precisam (conta nova/quebrada); skips nos ja OK.
- Spinner ao vivo + progresso `3/14`; backup so dos 4 saves mais recentes; sem 2º load de verificacao.

## [v22.3.47] — 2026-07-24
### Alterado
- **COMPLETO/CRIADOR:** sempre fazem limpeza (como REMOVER) e só depois aplicam — aplica limpo toda vez.
- **Troca de conta Epic:** sync do menu de video regrava **todas** as contas (save novo vinha com VideoOptions vazio → Alta qualidade / 60 FPS / tela preta longa).

## [v22.3.46] — 2026-07-24
### Corrigido
- **Menu COMPLETO voltava a Alta qualidade / 60 FPS / particula vazia:** o RL grava `VideoOptions` incompleto ao sair; o cliente rejeita o bloco e cai nos defaults (raios/clima ON).
- Patch agora detecta VideoOptions esparso/quebrado e regrava o perfil COMPLETO completo.
- Auto-heal do menu de video ao abrir o app se COMPLETO/CRIADOR estiver ativo (com o jogo fechado).

## [v22.3.45] — 2026-07-24
### Alterado
- **Admin automático:** o exe pede elevação UAC no arranque (`requireAdministrator` no manifesto).

## [v22.3.44] — 2026-07-24
### Corrigido
- Prompt e ícones: caracteres Unicode que viravam `?` no console Windows (`▶`, `●`, braille, etc.) trocados por ASCII seguro.

## [v22.3.43] — 2026-07-24
### Alterado
- **Rework visual total** do console: wordmark, boot sequence, painéis com fundo `#121212`, chips de status (modo / gravação / RL), cards numerados com tags FPS/STREAM.
- Telas de fluxo (steps, conclusão, launch options, corrigir erros) com painéis titulados, spinner e botão ENTER reforçado.

## [v22.3.42] — 2026-07-24
### Alterado
- **Troca COMPLETO ↔ CRIADOR:** ao mudar de modo, o app faz limpeza automática do INI (como REMOVER) e só depois aplica o novo perfil — não precisa mais ir ao menu Remover à mão.
- **CRIADOR (save):** limpa `VideoOptions` potato do COMPLETO (textura Higher, mundo Quality, RenderDetail Custom).

## [v22.3.41] — 2026-07-24
### Corrigido
- **COMPLETO — abre mas não carrega:** regressão v22.3.39 que travava o INI em somente-leitura. Em alguns PCs o RL precisa gravar o config no boot e fica preso no loading; REMOVER/restaurar “consertava”.
- COMPLETO volta a deixar o INI **gravável** (menu High Performance continua via patch do `.save`).

## [v22.3.40] — 2026-07-23
### Corrigido
- **COMPLETO — menu voltava a Alta qualidade / 60 FPS:** `ParticleDetail=Low` é inválido no cliente Epic (dropdown vazio) e o jogo rejeita o `VideoOptions` inteiro.
- Volta `ParticleDetail=Performance`; exige `bUncappedFramerate` + VideoOptions completos no verify do patch.
- Não injeta `bTranslucentArenaShaders` em saves que nunca tiveram o campo (evita rejeição do bloco Video).

## [v22.3.39] — 2026-07-23
### Alterado
- **COMPLETO — menu in-game forçado:** High Performance / Desempenho em Render/Texture/World, Particle=Low, Anti-Alias OFF, efeitos OFF, FPS Unlimited.
- `bTranslucentArenaShaders=False` (High Quality Shaders OFF).
- Sync do menu em **SaveDataEpic + SaveData** (Steam).
- INI do COMPLETO volta a ficar **somente-leitura** para o menu não regressar o potato.

## [v22.3.38] — 2026-07-22
### Corrigido
- **CRIADOR — Dynamic Shadows não apareciam:** o menu sozinho não basta; UE3 precisa de `DynamicLights` (+ cadeia de sombra).
- Liga no template: `DynamicShadows`, `DynamicLights`, `CompositeDynamicLights`, `DirectionalLightmaps`, `LightEnvironmentShadows`.
- Deixa de preservar `DynamicShadows=False` do INI antigo (sempre aplica o template).
- `CriadorForce` não mata mais luzes/sombras nos perfis derivados.

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
