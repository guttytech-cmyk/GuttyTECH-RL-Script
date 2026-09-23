using System.Globalization;

namespace GuttyRL;

/// <summary>Catálogo PT (padrão) / EN da interface WPF. Chave ausente volta o próprio texto.</summary>
internal static class UiCopy
{
    private static readonly Dictionary<string, string> Pt = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> En = new(StringComparer.Ordinal);

    static UiCopy()
    {
        Add("app.title", "GUTTYTECH · RL OPTIMIZER", "GUTTYTECH · RL OPTIMIZER");
        Add("brand.bar", "GUTTYTECH  /  RL OPTIMIZER", "GUTTYTECH  /  RL OPTIMIZER");
        Add("brand.product", "RL OPTIMIZER", "RL OPTIMIZER");
        Add("brand.tagline", "Otimizador competitivo do Rocket League", "Competitive Rocket League optimizer");
        Add("lang.tip", "Português / English", "Portuguese / English");
        Add("cap.min", "Minimizar", "Minimize");
        Add("cap.max", "Maximizar", "Maximize");
        Add("cap.restore", "Restaurar", "Restore");
        Add("cap.close", "Fechar", "Close");

        Add("nav.eyebrow", "NAVEGAÇÃO", "NAVIGATION");
        Add("nav.overview", "Visão Geral", "Overview");
        Add("nav.optimize", "Otimização", "Optimize");
        Add("nav.recovery", "Recuperação", "Recovery");
        Add("nav.system", "Sistema", "System");

        Add("dash.lastUpdate", "ÚLTIMA ATUALIZAÇÃO", "LAST UPDATE");
        Add("dash.refreshTip", "Atualiza o estado local e procura versão nova no GitHub", "Refresh local status and check GitHub for a new version");
        Add("dash.command", "CENTRO DE COMANDO", "COMMAND CENTER");
        Add("dash.overview", "VISÃO GERAL", "OVERVIEW");
        Add("dash.overviewSub", "Estado do perfil e atalhos para as operações principais.", "Profile status and shortcuts to the main actions.");
        Add("card.mode", "MODO ATIVO", "ACTIVE MODE");
        Add("card.folder", "PASTA", "FOLDER");
        Add("card.game", "JOGO", "GAME");
        Add("card.protect", "PROTEÇÃO", "PROTECTION");
        Add("card.admin", "ADMIN", "ADMIN");
        Add("card.path", "CAMINHO DO PERFIL", "PROFILE PATH");
        Add("dash.apply", "APLICAR PERFIL", "APPLY PROFILE");
        Add("dash.applySub", "Escolha o modo. O resto é guerra.", "Pick a mode. The rest is war.");
        Add("dash.details", "Ver detalhes →", "See details →");
        Add("tag.fps", "FPS MÁXIMO", "MAX FPS");
        Add("tag.stream", "STREAM / CAPTURA", "STREAM / CAPTURE");
        Add("mode.completo", "COMPLETO", "COMPLETO");
        Add("mode.criador", "CRIADOR", "CRIADOR");
        Add("mode.original", "ORIGINAL", "ORIGINAL");
        Add("dash.completoSub", "Ranked. Visual batata. FPS no talo.", "Ranked. Potato visuals. FPS uncapped.");
        Add("dash.b1", "▸ Texturas esmagadas até 2×2", "▸ Textures crushed down to 2×2");
        Add("dash.b2", "▸ Sem sombras, decals, efeitos e tessellation", "▸ No shadows, decals, effects, or tessellation");
        Add("dash.b3", "▸ LOD 2x2 · sombra 1 · watcher anti-rewrite", "▸ LOD 2×2 · shadow 1 · anti-rewrite watcher");
        Add("dash.creatorSub", "Visual pra câmera não ficar feia", "Visuals that still look fine on camera");
        Add("dash.c1", "▸ Visual pra câmera não ficar feia", "▸ Keeps the image readable on camera");
        Add("dash.c2", "▸ FPS + Apex/foliage/MSAA off — visual intacto", "▸ FPS up, Apex/foliage/MSAA off — image stays intact");
        Add("dash.c3", "▸ Estável pra clips e stream", "▸ Stable for clips and streams");
        Add("btn.detonate", "DETONAR COMPLETO", "DETONATE COMPLETO");
        Add("btn.creator", "ATIVAR CRIADOR", "ENABLE CRIADOR");

        Add("opt.eyebrow", "PERFIS DE PERFORMANCE", "PERFORMANCE PROFILES");
        Add("opt.title", "OTIMIZAÇÃO", "OPTIMIZE");
        Add("opt.sub", "Escolha o modo. Resolução, borda e backups ficam intactos — o resto é guerra.", "Pick a mode. Resolution, borderless, and backups stay — the rest is war.");
        Add("opt.completoSub", "Ranked. Throughput máximo. PC fraco ou sede de FPS.", "Ranked. Maximum throughput. Weak PC or you just want FPS.");
        Add("opt.b1", "▸ Bola, pitch, carros, banners e mapas: clamp até 2×2", "▸ Ball, pitch, cars, banners, and maps: clamped to 2×2");
        Add("opt.b2", "▸ Sombras, decals, folhagem, APEX e pós: OFF; partículas no mínimo", "▸ Shadows, decals, foliage, APEX, and post: OFF; particles at minimum");
        Add("opt.b3", "▸ Tessellation mínima + draw distance e LOD no limite", "▸ Minimum tessellation + draw distance and LOD pushed to the limit");
        Add("opt.b4", "▸ FPS ilimitado + VSync off + menu sincronizado", "▸ Uncapped FPS + VSync off + video menu synced");
        Add("opt.creatorSub", "Clipes, stream e gravação sem parecer batata total.", "Clips, streams, and recording without going full potato.");
        Add("opt.c1", "▸ Mantém leitura visual pra câmera não ficar feia", "▸ Keeps the picture readable so the camera does not look ugly");
        Add("opt.c2", "▸ Texturas e mundo mais altos — ainda empurra FPS", "▸ Higher textures and world detail — still pushes FPS");
        Add("opt.c3", "▸ FPS uncapped com perfil estável pra gravação", "▸ Uncapped FPS with a stable profile for recording");
        Add("opt.c4", "▸ Ideal pra conteúdo competitivo sem sacrificar a imagem", "▸ Built for competitive content without killing the image");
        Add("proto.title", "PROTOCOLO AUTOMÁTICO", "AUTOMATIC PROTOCOL");
        Add("proto.backup", "Backup", "Backup");
        Add("proto.backupSub", "Snapshot de segurança", "Safety snapshot");
        Add("proto.clean", "Limpeza", "Cleanup");
        Add("proto.cleanSub", "Remove resíduos antigos", "Removes old leftovers");
        Add("proto.apply", "Aplicação", "Apply");
        Add("proto.applySub", "Grava o novo perfil", "Writes the new profile");
        Add("proto.sync", "Sincronização", "Sync");
        Add("proto.syncSub", "Epic, Steam e menu", "Epic, Steam, and the menu");
        Add("btn.remove", "REMOVER OTIMIZAÇÃO DO SISTEMA", "REMOVE SYSTEM OPTIMIZATION");

        Add("rec.eyebrow", "QUANDO DER PROBLEMA", "WHEN SOMETHING BREAKS");
        Add("rec.title", "RECUPERAÇÃO", "RECOVERY");
        Add("rec.sub", "Escolha a correção certa pro seu problema.", "Pick the fix that matches your problem.");
        Add("rec.perm", "CORRIGIR PERMISSÕES", "FIX PERMISSIONS");
        Add("rec.permSub", "Desbloqueia a pasta do jogo quando o Windows impede de salvar.", "Unlocks the game folder when Windows blocks saving.");
        Add("rec.profile", "REPARAR PERFIL", "REPAIR PROFILE");
        Add("rec.profileSub", "Mantém seu modo (COMPLETO/CRIADOR) e conserta o menu de vídeo.", "Keeps your mode (COMPLETO/CRIADOR) and fixes the video menu.");
        Add("rec.presets", "RESTAURAR PRESETS", "RESTORE PRESETS");
        Add("rec.presetsSub", "Traz de volta carros e presets da garagem pelos backups locais.", "Brings cars and garage presets back from local backups.");
        Add("rec.boot", "RECUPERAR BOOT", "RECOVER BOOT");
        Add("rec.bootSub", "Jogo não abre / EAC 30005. Depois aplique o modo de novo.", "Game will not launch / EAC 30005. Apply the mode again afterwards.");
        Add("rec.eac", "REPARAR EASY ANTI-CHEAT", "REPAIR EASY ANTI-CHEAT");
        Add("rec.eacSub", "Erro 30005 CreateService 1072 — serviço EAC preso. Não mexe no INI. Se pedir, reinicie o PC.", "Error 30005 CreateService 1072 — EAC service stuck. Does not touch the INI. Restart the PC if asked.");
        Add("rec.save", "CORRIGIR SAVE — LOAD FAILURE", "FIX SAVE — LOAD FAILURE");
        Add("rec.saveSub", "Aviso LOAD FAILURE na Steam. Limpa o save quebrado. No aviso: NEW SAVE (oficial Epic) — depois use RESTAURAR PRESETS.", "LOAD FAILURE warning on Steam. Clears the broken save. On the prompt: NEW SAVE (official) — then use RESTORE PRESETS.");
        Add("rec.all", "CORRIGIR TUDO", "FIX EVERYTHING");
        Add("rec.allSub", "Último recurso pra abrir o jogo. Tira saves live (Best fica). Depois: RESTAURAR PRESETS → só então reaplica o modo.", "Last resort to make the game open. Pulls live saves (Best stays). Then: RESTORE PRESETS → only then reapply the mode.");

        Add("sys.eyebrow", "STEAM E EPIC GAMES", "STEAM AND EPIC GAMES");
        Add("sys.title", "SISTEMA", "SYSTEM");
        Add("sys.sub", "Comando de inicialização e logs de suporte pra mandar pro Gutty.", "Launch command and support logs to send to Gutty.");
        Add("sys.cmdTitle", "COMANDO DE INICIALIZAÇÃO PARA A STEAM E EPIC GAMES", "LAUNCH COMMAND FOR STEAM AND EPIC GAMES");
        Add("sys.cmdSub", "O mesmo comando funciona nas duas plataformas.", "The same command works on both platforms.");
        Add("btn.copy", "COPIAR COMANDO", "COPY COMMAND");
        Add("sys.flags", "O QUE CADA FLAG FAZ", "WHAT EACH FLAG DOES");
        Add("flag.nomovie", "Pula as intros / cutscenes na abertura do jogo.", "Skips intros / cutscenes when the game opens.");
        Add("flag.nosplash", "Remove a tela de splash (logo) antes do menu.", "Removes the splash screen (logo) before the menu.");
        Add("flag.nomansky", "Céu mais leve — menos carga na GPU.", "Lighter sky — less GPU load.");
        Add("flag.aa", "Desliga o anti-aliasing forçado — ganha FPS.", "Forces anti-aliasing off — gains FPS.");
        Add("flag.high", "Prioridade alta no Windows. Se o PC engasgar, tire só essa flag.", "High process priority. If the PC stutters, remove only this flag.");
        Add("sys.steamHelp", "Biblioteca → Rocket League → Propriedades → Opções de inicialização.", "Library → Rocket League → Properties → Launch options.");
        Add("sys.epicHelp", "Biblioteca → Rocket League → Gerenciar → Argumentos adicionais.", "Library → Rocket League → Manage → Additional command line arguments.");
        Add("sys.supportEyebrow", "SUPORTE GUTTY", "GUTTY SUPPORT");
        Add("sys.supportTitle", "LOGS PRA MANDAR PRO GUTTY", "LOGS TO SEND GUTTY");
        Add("sys.supportSub", "Junta logs do app, INI, saves e diagnóstico num zip. Manda pro Gutty — ele abre, lê e mata o erro.", "Packs app logs, the INI, saves, and a diagnostic into a zip. Send it to Gutty — he opens it and kills the bug.");
        Add("sys.supportSteps", "1 gera o zip  ·  2 manda pro Gutty", "1 build the zip  ·  2 send it to Gutty");
        Add("btn.pack", "GERAR PACOTE", "BUILD PACK");
        Add("btn.cancel", "CANCELAR", "CANCEL");
        Add("btn.refresh", "ATUALIZAR", "REFRESH");
        Add("btn.searching", "BUSCANDO…", "CHECKING…");

        Add("busy.eyebrow", "OPERAÇÃO EM ANDAMENTO", "OPERATION IN PROGRESS");
        Add("busy.steps", "ETAPAS", "STEPS");
        Add("busy.s1", "Backup do perfil e presets", "Profile and preset backup");
        Add("busy.s2", "Limpar modo anterior", "Clear the previous mode");
        Add("busy.s3", "Gravar otimização no INI", "Write the optimization to the INI");
        Add("busy.s4", "Sincronizar menu de vídeo", "Sync the video menu");
        Add("busy.s4sub", "Etapa mais longa — patch nas contas Epic/Steam", "Longest step — patch on Epic/Steam accounts");
        Add("busy.s5", "Validar e finalizar", "Validate and finish");

        Add("toast.github", "ATUALIZAÇÃO GITHUB", "GITHUB UPDATE");
        Add("toast.new", "VERSÃO NOVA", "NEW VERSION");
        Add("toast.what", "O QUE MUDOU", "WHAT CHANGED");
        Add("toast.later", "DEPOIS", "LATER");
        Add("toast.download", "BAIXAR", "DOWNLOAD");
        Add("toast.downloading", "Baixando para o Desktop…", "Downloading to the Desktop…");
        Add("toast.downFail", "Download falhou — abrindo a página da release.", "Download failed — opening the release page.");
        Add("toast.detailDefault", "Baixa o .exe novo, fecha este app e abre o arquivo do Desktop.", "Download the new .exe, close this app, and open the file on the Desktop.");
        Add("toast.downloadedSub", "Download pronto no Desktop. Feche este app e abra o .exe novo.", "Download is on the Desktop. Close this app and open the new .exe.");
        Add("toast.title", "GuttyTECH — Atualização", "GuttyTECH — Update");

        Add("log.changelog", "CHANGELOG", "CHANGELOG");
        Add("log.new", "NOVA", "NEW");
        Add("log.gotit", "ENTENDI", "GOT IT");
        Add("log.titleFallback", "O QUE MUDOU", "WHAT CHANGED");
        Add("log.subFallback", "Atualizou? Aqui está o que mudou nas últimas versões.", "Just updated? Here is what changed in the latest versions.");
        Add("log.since", "O que mudou desde a v{0}:", "What changed since v{0}:");
        Add("log.updated", "Atualizou? Aqui está o que mudou nas versões novas.", "Just updated? Here is what changed in the new versions.");
        Add("log.already", "Você já está na última versão. Resumo do que mudou:", "You are already on the latest version. Summary of what changed:");
        Add("log.windowTitle", "GuttyTECH — Changelog", "GuttyTECH — Changelog");

        Add("op.preparing", "PREPARANDO OPERAÇÃO", "PREPARING OPERATION");
        Add("prog.wait", "Aguarde um instante", "One moment");
        Add("state.loading", "CARREGANDO", "LOADING");
        Add("state.reading", "Lendo perfil do Rocket League", "Reading the Rocket League profile");
        Add("hint.writable", "A verificar permissão da pasta", "Checking folder permission");
        Add("hint.game", "A verificar se o jogo está aberto", "Checking if the game is open");
        Add("hint.protect", "A verificar bloqueio do perfil", "Checking profile protection");
        Add("hint.admin", "A verificar direitos de administrador", "Checking administrator rights");
        Add("dash.pathSearching", "Localizando TASystemSettings.ini...", "Locating TASystemSettings.ini...");
        Add("dash.now", "AGORA", "NOW");
        Add("dash.pathMissing", "Perfil ainda não encontrado — abra o Rocket League uma vez.", "Profile not found yet — open Rocket League once.");
        Add("dash.updated", "ATUALIZADO {0}", "UPDATED {0}");
        Add("card.ok", "OK", "OK");
        Add("card.blocked", "BLOQUEADA", "BLOCKED");
        Add("hint.canWrite", "Podemos salvar o perfil", "We can save the profile");
        Add("hint.noPerm", "Sem permissão — Corrigir", "No permission — Fix");
        Add("card.open", "ABERTO", "OPEN");
        Add("card.closed", "FECHADO", "CLOSED");
        Add("hint.closeGame", "Feche o jogo pra aplicar", "Close the game to apply");
        Add("hint.canApply", "Pode aplicar um modo", "You can apply a mode");
        Add("card.active", "ATIVA", "ON");
        Add("hint.watcher", "Watcher anti-rewrite · vídeo livre", "Anti-rewrite watcher · video menu stays writable");
        Add("card.modeOnly", "MODO", "MODE");
        Add("hint.watcherStarting", "Modo ativo — watcher a arrancar", "Mode active — watcher starting");
        Add("card.off", "OFF", "OFF");
        Add("hint.noMode", "Sem modo Gutty aplicado", "No Gutty mode applied");
        Add("card.yes", "SIM", "YES");
        Add("card.no", "NÃO", "NO");
        Add("hint.adminOk", "Rodando como admin", "Running as admin");
        Add("hint.adminBad", "ERRO: abra como administrador", "ERROR: run as administrator");

        Add("state.fps", "FPS máximo ativo", "Max FPS active");
        Add("state.creator", "Visual + perf ativo", "Visual + performance active");
        Add("state.original", "Original (não otimizado)", "Original (not optimized)");
        Add("state.legacy", "Otimizado (versão antiga)", "Optimized (old version)");
        Add("state.missing", "Perfil do Rocket League ainda não localizado", "Rocket League profile not located yet");
        Add("state.noini", "INI ausente — abra o Rocket League uma vez", "INI missing — open Rocket League once");

        Add("fb.statusTitle", "STATUS ATUALIZADO", "STATUS UPDATED");
        Add("fb.statusBody", "Perfil, acesso e processo do Rocket League foram verificados.", "Profile, access, and the Rocket League process were checked.");
        Add("fb.uiErrorTitle", "ERRO NA INTERFACE", "INTERFACE ERROR");
        Add("fb.uiErrorBody", "A ação foi interrompida com segurança. Consulte {0}", "The action was stopped safely. See {0}");
        Add("fb.githubDownTitle", "GITHUB INDISPONÍVEL", "GITHUB UNAVAILABLE");
        Add("fb.newTitle", "VERSÃO NOVA NO GITHUB", "NEW VERSION ON GITHUB");
        Add("fb.newBody", "{0} Veja o changelog no popup e use BAIXAR.", "{0} See the changelog in the popup and use DOWNLOAD.");
        Add("fb.latestTitle", "ÚLTIMA VERSÃO", "LATEST VERSION");

        Add("op.completo", "APLICANDO MODO COMPLETO", "APPLYING COMPLETO MODE");
        Add("op.criador", "APLICANDO MODO CRIADOR", "APPLYING CRIADOR MODE");
        Add("op.remove", "REMOVENDO OTIMIZAÇÃO", "REMOVING OPTIMIZATION");
        Add("op.copy", "COPIANDO COMANDO", "COPYING COMMAND");
        Add("op.perm", "CORRIGINDO PERMISSÕES", "FIXING PERMISSIONS");
        Add("op.profile", "REPARANDO PERFIL", "REPAIRING PROFILE");
        Add("op.boot", "RECUPERANDO BOOT", "RECOVERING BOOT");
        Add("op.eac", "REPARANDO EASY ANTI-CHEAT", "REPAIRING EASY ANTI-CHEAT");
        Add("op.diag", "GERANDO PACOTE DE LOGS", "BUILDING LOG PACK");
        Add("op.all", "CORRIGINDO TUDO", "FIXING EVERYTHING");
        Add("op.presets", "RESTAURANDO PRESETS", "RESTORING PRESETS");
        Add("op.save", "CORRIGINDO SAVE / LOAD FAILURE", "FIXING SAVE / LOAD FAILURE");
        Add("op.generic", "EXECUTANDO OPERAÇÃO", "RUNNING OPERATION");

        Add("confirm.removeTitle", "REMOVER OTIMIZAÇÃO DO SISTEMA?", "REMOVE SYSTEM OPTIMIZATION?");
        Add("confirm.removeBody", "Para o watcher automático, restaura o INI stock/original, limpa o cache Epic e remove marcas Gutty. Presets e garagem ficam no cofre. Não apaga a pasta de backups.", "Stops the automatic watcher, restores the stock/original INI, clears the Epic cache, and removes Gutty marks. Presets and the garage stay in the vault. Does not delete the backup folder.");
        Add("confirm.removeAction", "REMOVER DO SISTEMA", "REMOVE FROM SYSTEM");
        Add("confirm.bootTitle", "JOGO NÃO ABRE — RECUPERAR BOOT?", "GAME WILL NOT OPEN — RECOVER BOOT?");
        Add("confirm.bootBody", "Fecha o RL, remove o otimizador (INI stock), limpa boot-killers, põe saves suspeitos em quarentena e repara Easy Anti-Cheat (erro 30005). A garagem fica no Best — depois use RESTAURAR PRESETS. Só reaplique COMPLETO/CRIADOR depois do jogo abrir.", "Closes RL, removes the optimizer (stock INI), clears boot-killers, quarantines suspicious saves, and repairs Easy Anti-Cheat (error 30005). The garage stays in Best — then use RESTORE PRESETS. Reapply COMPLETO/CRIADOR only after the game opens.");
        Add("confirm.bootAction", "DESBLOQUEAR JOGO", "UNLOCK GAME");
        Add("confirm.eacTitle", "ERRO EAC 30005 — REPARAR ANTI-CHEAT?", "EAC ERROR 30005 — REPAIR ANTI-CHEAT?");
        Add("confirm.eacBody", "Reinstala o serviço EasyAntiCheat_EOS (CreateService 1072). Não mexe no INI. Se o Windows marcou o serviço para apagar, pode ser preciso reiniciar o PC.", "Reinstalls the EasyAntiCheat_EOS service (CreateService 1072). Does not touch the INI. If Windows marked the service for deletion, you may need to restart the PC.");
        Add("confirm.eacAction", "REPARAR EAC", "REPAIR EAC");
        Add("confirm.allTitle", "JOGO NÃO ABRE — CORRIGIR TUDO?", "GAME WILL NOT OPEN — FIX EVERYTHING?");
        Add("confirm.allBody", "Caminho nuclear: prioridade é o Rocket League voltar a abrir. Tira os saves live (garagem fica no Best). Depois que o jogo abrir: use RESTAURAR PRESETS. NÃO reaplica COMPLETO/CRIADOR em cima do perfil partido.", "Nuclear path: the priority is getting Rocket League to open. Pulls live saves (garage stays in Best). After the game opens: use RESTORE PRESETS. Does NOT reapply COMPLETO/CRIADOR on a broken profile.");
        Add("confirm.allAction", "DESBLOQUEAR AGORA", "UNLOCK NOW");
        Add("confirm.presetsTitle", "RESTAURAR PRESETS?", "RESTORE PRESETS?");
        Add("confirm.presetsBody", "Vamos repor a maior garagem guardada (cofre Best) nas contas Epic/Steam e quarentenar o remote Steam Cloud. Depois abra o jogo OFFLINE na 1ª sessão.", "Restores the largest saved garage (Best vault) onto Epic/Steam accounts and quarantines the Steam Cloud remote. Then open the game OFFLINE for the first session.");
        Add("confirm.presetsAction", "RESTAURAR BACKUP", "RESTORE BACKUP");
        Add("confirm.saveTitle", "LOAD FAILURE — LIMPAR SAVE STEAM?", "LOAD FAILURE — CLEAR STEAM SAVE?");
        Add("confirm.saveBody", "Fecha Steam+RL, desliga Cloud no localconfig, limpa SaveData (sem repor Best — isso fazia o aviso voltar). Depois abre o RL e se o aviso aparecer clica NEW SAVE (tutorial às vezes é normal; rank/itens online ficam). Só depois RESTAURAR PRESETS.", "Closes Steam+RL, turns Cloud off in localconfig, clears SaveData (does not put Best back — that made the warning return). Then open RL and if the prompt appears click NEW SAVE (a tutorial can be normal; rank/online items stay). Only then RESTORE PRESETS.");
        Add("confirm.saveAction", "LIMPAR SAVE", "CLEAR SAVE");

        Add("upd.overlayTitle", "BUSCANDO ATUALIZAÇÕES", "CHECKING FOR UPDATES");
        Add("upd.overlayBody", "Consultando o GitHub pela versão mais recente. O botão pulsa até responder.", "Asking GitHub for the latest version. The button pulses until it answers.");
        Add("upd.footerChecking", "Procurando versão nova no GitHub…", "Looking for a new version on GitHub…");
        Add("upd.footerNew", "NOVA {0} · local {1}", "NEW {0} · local {1}");
        Add("upd.footerOk", "ATUALIZADO {0} · {1}", "UPDATED {0} · {1}");
        Add("upd.githubStatus", "GitHub respondeu {0}. Tente de novo em instantes.", "GitHub returned {0}. Try again in a moment.");
        Add("upd.githubParse", "Não consegui ler as releases no GitHub.", "Could not read the releases on GitHub.");
        Add("upd.githubLatest", "Não consegui ler a release mais recente no GitHub.", "Could not read the latest release on GitHub.");
        Add("upd.newMsg", "Tem versão nova no GitHub: v{0} (você está em v{1}). Abaixo está o que mudou desde a sua versão.", "There is a new version on GitHub: v{0} (you are on v{1}). Below is what changed since your version.");
        Add("upd.currentMsg", "Você já está na última: v{0}.", "You are already on the latest: v{0}.");
        Add("upd.offline", "Sem conexão com o GitHub agora. Verifique a internet e tente ATUALIZAR de novo.", "No connection to GitHub right now. Check your internet and try REFRESH again.");
        Add("upd.downloadingFile", "Baixando {0}…", "Downloading {0}…");
        Add("upd.downloadDone", "Download concluído no Desktop", "Download finished on the Desktop");

        Add("res.cancelledTitle", "OPERAÇÃO CANCELADA", "OPERATION CANCELLED");
        Add("res.cancelledBody", "Nenhuma nova operação foi iniciada.", "No new operation was started.");
        Add("res.crashTitle", "FALHA INESPERADA", "UNEXPECTED FAILURE");
        Add("res.crashBody", "O motor interrompeu a operação com segurança. Consulte o log em {0}", "The engine stopped the operation safely. See the log at {0}");
        Add("prog.safe", "Preparando ambiente seguro", "Preparing a safe environment");
        Add("prog.cmdPrep", "Preparando comando unificado", "Preparing the unified command");
        Add("prog.cmdWrite", "A gravar no Desktop e copiar", "Writing to the Desktop and copying");
        Add("prog.cmdReady", "Comando pronto", "Command ready");
        Add("prog.cmdDesktop", "Salvo no Desktop", "Saved on the Desktop");
        Add("res.copyTitle", "COMANDO COPIADO", "COMMAND COPIED");
        Add("res.copyBody", "Colado na área de transferência (sessão do usuário):\n{0}\n\nSteam e Epic usam o mesmo comando.", "Copied to the clipboard (your user session):\n{0}\n\nSteam and Epic use the same command.");
        Add("res.copyBodyFile", "Colado na área de transferência (sessão do usuário):\n{0}\nTambém salvo em: {1}\n\nSteam e Epic usam o mesmo comando.", "Copied to the clipboard (your user session):\n{0}\nAlso saved at: {1}\n\nSteam and Epic use the same command.");
        Add("res.copyDesktopTitle", "COMANDO NO DESKTOP", "COMMAND ON THE DESKTOP");
        Add("res.copyDesktopBody", "O Windows bloqueou a área de transferência (app em admin). Abra o arquivo e copie (Ctrl+C):\n{0}\n\n{1}", "Windows blocked the clipboard (app is running as admin). Open the file and copy (Ctrl+C):\n{0}\n\n{1}");
        Add("res.copyFailTitle", "NÃO FOI POSSÍVEL COPIAR", "COULD NOT COPY");
        Add("res.copyFailBody", "Copie manualmente:\n{0}", "Copy it manually:\n{0}");
        Add("prog.diagRead", "Lendo perfil e logs do otimizador", "Reading the profile and optimizer logs");
        Add("prog.diagPack", "Empacotando diagnóstico e INI", "Packing the diagnostic and INI");
        Add("prog.diagReady", "Pacote pronto no Desktop", "Pack ready on the Desktop");
        Add("prog.diagFail", "Falha ao gerar pacote", "Failed to build the pack");
        Add("prog.closing", "Fechando Rocket League", "Closing Rocket League");
        Add("prog.closingDetail", "O jogo sobrescreve o perfil se ficar aberto", "The game overwrites the profile if it stays open");
        Add("res.closeFailTitle", "NÃO CONSEGUI FECHAR O JOGO", "COULD NOT CLOSE THE GAME");
        Add("res.closeFailBody", "Feche o Rocket League manualmente e tente de novo — o jogo sobrescreve o INI ao sair.", "Close Rocket League manually and try again — the game overwrites the INI when it exits.");
        Add("prog.prepMode", "Criando backup e limpando perfil anterior", "Creating a backup and clearing the previous profile");
        Add("prog.prepModeDetail", "Preparando motor Gutty", "Preparing the Gutty engine");
        Add("prog.prepRemove", "Parando watcher e preservando presets", "Stopping the watcher and keeping presets");
        Add("prog.prepPresets", "Localizando o melhor backup de garagem", "Finding the best garage backup");
        Add("prog.prepSave", "Analisando saves Steam e remote Cloud", "Analyzing Steam saves and Cloud remote");
        Add("prog.prepGeneric", "Analisando INI, saves e permissões", "Analyzing the INI, saves, and permissions");
        Add("prog.validate", "Validando resultado", "Validating the result");
        Add("prog.validateDetail", "Lendo perfil aplicado", "Reading the applied profile");
        Add("prog.done", "Operação concluída", "Operation finished");
        Add("prog.attention", "Ação requer atenção", "Action needs attention");
        Add("res.rebootTitle", "REINICIE O PC", "RESTART THE PC");
        Add("res.rebootBody", "O Windows marcou o Easy Anti-Cheat para apagar (1072). Reinicie e abra o RL. Se continuar: Verificar arquivos na Epic.", "Windows marked Easy Anti-Cheat for deletion (1072). Restart and open RL. If it continues: Verify files on Epic.");
        Add("res.noProfileTitle", "PERFIL DO JOGO NÃO ENCONTRADO", "GAME PROFILE NOT FOUND");
        Add("res.noProfileBody", "Abra o Rocket League uma vez para criar a pasta TAGame\\Config e tente novamente.", "Open Rocket League once so it creates the TAGame\\Config folder, then try again.");
        Add("res.noModeTitle", "NENHUM MODO ATIVO", "NO MODE ACTIVE");
        Add("res.noModeBody", "Se o jogo não abre, use RECUPERAR BOOT ou CORRIGIR TUDO. Se abre, aplique COMPLETO/CRIADOR e depois repare.", "If the game will not open, use RECOVER BOOT or FIX EVERYTHING. If it opens, apply COMPLETO/CRIADOR and then repair.");
        Add("res.okCompletoTitle", "MODO COMPLETO APLICADO", "COMPLETO MODE APPLIED");
        Add("res.okCompletoAgain", "Perfil reaplicado. INI + watcher OK (menu de vídeo sincroniza quando houver save leve).", "Profile reapplied. INI + watcher OK (the video menu syncs when a light save exists).");
        Add("res.okCompletoFresh", "FPS máximo ativado, INI gravado e backup preservado.", "Max FPS enabled, INI written, and backup kept.");
        Add("res.okCriadorTitle", "MODO CRIADOR APLICADO", "CRIADOR MODE APPLIED");
        Add("res.okCriadorAgain", "Perfil visual reaplicado. INI + watcher OK (menu de vídeo sincroniza quando houver save leve).", "Visual profile reapplied. INI + watcher OK (the video menu syncs when a light save exists).");
        Add("res.okCriadorFresh", "Performance forte com visual competitivo. INI gravado e backup preservado.", "Strong performance with competitive visuals. INI written and backup kept.");
        Add("res.okRemoveTitle", "OTIMIZAÇÃO REMOVIDA DO SISTEMA", "OPTIMIZATION REMOVED FROM THE SYSTEM");
        Add("res.okRemoveBody", "Watcher parado, INI stock, cache Epic limpo. Presets/garagem intactos. Se colou flags em Steam/Epic, remova-as manualmente.", "Watcher stopped, stock INI, Epic cache cleared. Presets/garage intact. If you pasted flags into Steam/Epic, remove them yourself.");
        Add("res.okPermTitle", "ACESSO RESTABELECIDO", "ACCESS RESTORED");
        Add("res.okPermYes", "A pasta do jogo está gravável novamente.", "The game folder is writable again.");
        Add("res.okPermMaybe", "O reparo terminou, mas o Windows ainda pode exigir uma exceção no Defender.", "The repair finished, but Windows may still require a Defender exception.");
        Add("res.okProfileTitle", "PERFIL REPARADO", "PROFILE REPAIRED");
        Add("res.okProfileBody", "INI, menu de vídeo e cache foram sincronizados sem remover o modo aplicado.", "INI, video menu, and cache were synced without removing the applied mode.");
        Add("res.okBootTitle", "JOGO DESBLOQUEADO", "GAME UNLOCKED");
        Add("res.okBootBody", "INI stock + boot-safe + saves em quarentena + EAC OK. Próximo passo obrigatório: RESTAURAR PRESETS (este caminho tirou os saves live de propósito). Depois abra o RL 1× e só então reaplique o modo.", "Stock INI + boot-safe + saves quarantined + EAC OK. Required next step: RESTORE PRESETS (this path pulled live saves on purpose). Then open RL once and only then reapply the mode.");
        Add("res.okEacTitle", "EAC REPARADO", "EAC REPAIRED");
        Add("res.okEacBody", "Serviço EasyAntiCheat_EOS reinstalado. Abra o Rocket League pela Epic/Steam.", "EasyAntiCheat_EOS service reinstalled. Open Rocket League from Epic/Steam.");
        Add("res.okAllTitle", "CORREÇÃO NUCLEAR CONCLUÍDA", "NUCLEAR FIX FINISHED");
        Add("res.okAllBody", "Boot desbloqueado (não reaplica o otimizador). AGORA use RESTAURAR PRESETS — os saves live foram pra quarentena de propósito. Só depois que a garagem voltar: abra o RL e reaplique COMPLETO/CRIADOR.", "Boot unlocked (does not reapply the optimizer). NOW use RESTORE PRESETS — live saves were quarantined on purpose. Only after the garage is back: open RL and reapply COMPLETO/CRIADOR.");
        Add("res.okPresetsTitle", "PRESETS RESTAURADOS", "PRESETS RESTORED");
        Add("res.okPresetsBody", "Garagem reposta + Steam Cloud remote em quarentena. Abra OFFLINE na 1ª sessão; Steam: cloud OFF temporário.", "Garage restored + Steam Cloud remote quarantined. Open OFFLINE for the first session; Steam: cloud OFF temporarily.");
        Add("res.okSaveTitle", "SAVE STEAM LIMPO", "STEAM SAVE CLEARED");
        Add("res.okSaveBody", "Steam fechada, Cloud OFF, pasta SaveData limpa (sem repor Best). Abra o RL → se LOAD FAILURE, use NEW SAVE (tutorial pode aparecer; itens online ficam). Depois RESTAURAR PRESETS. Guia no Desktop.", "Steam closed, Cloud OFF, SaveData folder cleared (Best was not put back). Open RL → if LOAD FAILURE, use NEW SAVE (a tutorial can appear; online items stay). Then RESTORE PRESETS. Guide on the Desktop.");
        Add("res.okGenericTitle", "OPERAÇÃO CONCLUÍDA", "OPERATION FINISHED");
        Add("res.okGenericBody", "O estado do otimizador foi atualizado.", "The optimizer state was updated.");
        Add("res.blockedTitle", "AÇÃO BLOQUEADA PELO JOGO", "ACTION BLOCKED BY THE GAME");
        Add("res.blockedBody", "Feche o Rocket League e execute a operação novamente.", "Close Rocket League and run the operation again.");
        Add("res.lockedTitle", "PASTA AINDA BLOQUEADA", "FOLDER STILL BLOCKED");
        Add("res.lockedBody", "Execute CORRIGIR PERMISSÕES como administrador ou permita o aplicativo no Acesso Controlado a Pastas.", "Run FIX PERMISSIONS as administrator or allow the app in Controlled Folder Access.");
        Add("res.gameMissingTitle", "JOGO NÃO ENCONTRADO", "GAME NOT FOUND");
        Add("res.gameBadTitle", "INSTALAÇÃO INCOMPLETA", "INCOMPLETE INSTALL");
        Add("res.installMissing", "Epic/Steam → Verificar arquivos (RL não instalado). Não use Recuperar Boot. Depois que o RL abrir sozinho pela Epic/Steam, use RESTAURAR PRESETS.", "Epic/Steam → Verify files (RL is not installed). Do not use Recover Boot. After RL opens on its own from Epic/Steam, use RESTORE PRESETS.");
        Add("res.installIncomplete", "Epic/Steam → Verificar arquivos (instalação incompleta / EAC ausente). Depois que o RL abrir sozinho pela Epic/Steam, use RESTAURAR PRESETS.", "Epic/Steam → Verify files (incomplete install / EAC missing). After RL opens on its own from Epic/Steam, use RESTORE PRESETS.");
        Add("res.profilePartialTitle", "REPARO PARCIAL", "PARTIAL REPAIR");
        Add("res.profilePartialBody", "O modo foi detectado, mas INI, save ou cache não sincronizou por completo.", "The mode was detected, but the INI, save, or cache did not fully sync.");
        Add("res.bootPartialTitle", "RECUPERAÇÃO INCOMPLETA", "INCOMPLETE RECOVERY");
        Add("res.bootPartialBody", "Não foi possível restaurar todos os arquivos stock/saves/EAC. Se viu erro 30005, reinicie o PC e use REPARAR EAC.", "Could not restore every stock file/save/EAC piece. If you saw error 30005, restart the PC and use REPAIR EAC.");
        Add("res.eacFailTitle", "EAC AINDA FALHOU", "EAC STILL FAILED");
        Add("res.eacFailBody", "Reinicie o PC (serviço marcado para apagar — 1072). Depois Epic → Verificar arquivos e abra o jogo.", "Restart the PC (service marked for deletion — 1072). Then Epic → Verify files and open the game.");
        Add("res.noBackupTitle", "SEM BACKUP RECUPERÁVEL", "NO RECOVERABLE BACKUP");
        Add("res.noBackupBody", "Não há save grande em Backups/Presets/Best. Sem esse arquivo local não dá para recuperar a garagem.", "There is no large save in Backups/Presets/Best. Without that local file the garage cannot be recovered.");
        Add("res.saveRiskTitle", "SAVE AINDA EM RISCO", "SAVE STILL AT RISK");
        Add("res.saveRiskBody", "Não consegui limpar o SaveData/Steam Cloud. Feche Steam + RL e tente de novo; se falhar, use RECUPERAR BOOT.", "Could not clear SaveData/Steam Cloud. Close Steam + RL and try again; if it fails, use RECOVER BOOT.");
        Add("res.failTitle", "OPERAÇÃO NÃO CONCLUÍDA", "OPERATION NOT FINISHED");
        Add("res.failBody", "O motor preservou o backup, mas uma etapa falhou. Gere o pacote de logs e manda pro Gutty.", "The engine kept the backup, but a step failed. Build the log pack and send it to Gutty.");
        Add("res.packFailTitle", "PACOTE NÃO GERADO", "PACK NOT BUILT");
        Add("res.packWarnTitle", "PACOTE PRONTO — TEM ALERTAS", "PACK READY — HAS ALERTS");
        Add("res.packOkTitle", "PACOTE PRONTO NO DESKTOP", "PACK READY ON THE DESKTOP");
        Add("raw.pass", "{0}", "{0}");
        Add("pack.summaryWarn", "Zip no Desktop com alertas ({0} arquivos). Manda o ZIP pro Gutty: {1}", "Zip on the Desktop with alerts ({0} files). Send the ZIP to Gutty: {1}");
        Add("pack.summaryOk", "Zip no Desktop pronto ({0} arquivos). Manda o ZIP pro Gutty: {1}", "Zip ready on the Desktop ({0} files). Send the ZIP to Gutty: {1}");
        Add("pack.summaryFail", "Não consegui gerar o pacote de logs ({0}). Tente de novo ou mande o log.txt da pasta GuttyTECH.", "Could not build the log pack ({0}). Try again or send log.txt from the GuttyTECH folder.");
        Add("file.steam", "Steam: Biblioteca → Rocket League → Engrenagem → Propriedades → Opções de inicialização", "Steam: Library → Rocket League → Gear → Properties → Launch options");
        Add("file.epic", "Epic: Biblioteca → Rocket League → … → Gerenciar → Argumentos adicionais", "Epic: Library → Rocket League → … → Manage → Additional arguments");

        Add("prog.backup", "Backup do perfil", "Profile backup");
        Add("prog.backupDetail", "Preservando INI e presets da garagem", "Keeping the INI and garage presets");
        Add("prog.unlock", "Destravando arquivo", "Unlocking the file");
        Add("prog.unlockDetail", "Removendo somente-leitura se existir", "Removing read-only if it is set");
        Add("prog.clean", "Limpando modo anterior", "Clearing the previous mode");
        Add("prog.cleanDetail", "Reset limpo antes de gravar o modo", "Clean reset before writing the mode");
        Add("prog.write", "Gravando otimização", "Writing the optimization");
        Add("prog.bootcheck", "Validando boot-safe", "Validating boot-safe");
        Add("prog.bootcheckDetail", "Checando chaves que travam o jogo", "Checking keys that freeze the game");
        Add("prog.sync", "Sincronizando menu de vídeo", "Syncing the video menu");
        Add("prog.syncDetail", "Patch nas contas — esta etapa demora mais", "Patching accounts — this step takes longer");
        Add("prog.account", "Conta {0}/{1}", "Account {0}/{1}");
        Add("prog.presets", "Reforçando presets", "Reinforcing presets");
        Add("prog.presetsDetail", "Garantindo que a garagem não sumiu", "Making sure the garage did not vanish");
        Add("prog.finish", "Finalizando", "Finishing");
        Add("prog.finishDetail", "INI liberado pra o menu do jogo", "INI left writable for the game menu");

        Add("fatal.title", "GUTTYTECH · FALHA SEGURA", "GUTTYTECH · SAFE FAILURE");
        Add("fatal.bar", "GUTTYTECH  /  PROTEÇÃO DE STARTUP", "GUTTYTECH  /  STARTUP PROTECTION");
        Add("fatal.close", "FECHAR COM SEGURANÇA", "CLOSE SAFELY");
        Add("fatal.ui", "Erro na interface do GuttyRL.", "GuttyRL interface error.");
        Add("fatal.window", "Falha ao abrir a janela do GuttyRL.", "Failed to open the GuttyRL window.");
        Add("fatal.unexpected", "Erro inesperado no GuttyRL.", "Unexpected GuttyRL error.");
        Add("fatal.windowsOnly", "Este GuttyRL é exclusivo para Windows.\n\nSO detectado: {0}", "This GuttyRL build is Windows-only.\n\nDetected OS: {0}");
        Add("fatal.arch", "Este GuttyRL é só para Windows 64-bit (x64).\n\nSeu processador: {0}\n\nUse um PC Windows x64.", "This GuttyRL build is Windows 64-bit (x64) only.\n\nYour processor: {0}\n\nUse a 64-bit Windows PC.");
        Add("fatal.cannot", "O GuttyRL encontrou um erro e não pode continuar.", "GuttyRL hit an error and cannot continue.");
    }

    public static string Get(string? key)
    {
        if (string.IsNullOrEmpty(key))
            return "";

        Dictionary<string, string> table = UiLanguage.IsEnglish ? En : Pt;
        if (table.TryGetValue(key, out string? text))
            return text;
        if (Pt.TryGetValue(key, out text))
            return text;
        return key;
    }

    public static string Format(string key, params object[] args)
    {
        string template = Get(key);
        if (args.Length == 0)
            return template;

        try
        {
            return string.Format(CultureInfo.InvariantCulture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    public static IReadOnlyList<string> Audit()
    {
        var bad = new List<string>();
        if (Pt.Count != En.Count)
            bad.Add("count " + Pt.Count + "/" + En.Count);

        foreach (KeyValuePair<string, string> pair in Pt)
        {
            if (!En.TryGetValue(pair.Key, out string? en) || en.Length == 0 || pair.Value.Length == 0)
                bad.Add(pair.Key);
        }

        return bad;
    }

    private static void Add(string key, string pt, string en)
    {
        Pt[key] = pt;
        En[key] = en;
    }
}
