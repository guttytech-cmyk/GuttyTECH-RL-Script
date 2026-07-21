================================================================================
  GUTTYTECH - ROCKET LEAGUE INI OPTIMIZER  v22.3  (TESSERACT)
================================================================================

Otimizador do arquivo TASystemSettings.ini do Rocket League (Unreal Engine 3),
com 3 modos e 1 clique. Funciona em qualquer PC com Windows 10/11.

  [1] COMPLETO  -> FPS maximo, graficos minimos. Para competitivo / PC fraco.
  [2] CRIADOR   -> Otimizado SEM destruir o visual. Para streamers/YouTubers.
  [3] REMOVER   -> Restaura o original (ou o padrao de fabrica) e destrava.
  [4] LAUNCH OPTIONS -> Copia o melhor comando de inicializacao (Steam/Epic)
                        pro clipboard; voce so cola na plataforma.


--------------------------------------------------------------------------------
 COMO USAR  (2 jeitos - escolha um)
--------------------------------------------------------------------------------

JEITO A - Pasta + .bat  (mais compativel, recomendado):
  1. Mantenha esta pasta inteira junta (GuttyRL.bat + a pasta "templates").
  2. Feche o Rocket League.
  3. De 2 cliques em "GuttyRL.bat".
  4. Escolha 1, 2 ou 3 no menu. Pronto.

JEITO B - Executavel unico (RECOMENDADO pra mandar pro cliente):
  - Mande SO o "GuttyTECH_RL.exe". E um app de console .NET 9 autossuficiente:
      * Arquivo unico (os 3 templates estao embutidos dentro dele).
      * O cliente NAO precisa ter .NET instalado, nem a pasta "templates".
      * Da 2 cliques -> abre a janela do menu (igual ao .bat).
  - Na 1a vez pode aparecer o SmartScreen ("Windows protegeu o seu PC"):
    Mais informacoes -> Executar assim mesmo.
  - Para (re)gerar o GuttyTECH_RL.exe na SUA maquina: de 2 cliques em "build_exe.bat"
    (precisa do .NET 9 SDK instalado SO na sua maquina de build, nao no cliente).

MODO AVANCADO (linha de comando / automacao):
  GuttyRL.bat COMPLETO     -> aplica direto, sem menu
  GuttyRL.bat CRIADOR
  GuttyRL.bat REMOVER


--------------------------------------------------------------------------------
 IMPORTANTE: AJUSTE O JOGO 1 VEZ  (passo que faz a otimizacao "colar")
--------------------------------------------------------------------------------

O Rocket League le o .ini junto com as opcoes que voce escolhe DENTRO do jogo.
O otimizador trava o arquivo (somente-leitura) para o jogo nao apagar as
mudancas - mas voce ainda deve deixar as opcoes de video coerentes 1 vez:

  >> MODO COMPLETO (FPS maximo):
     Opcoes > Video (aplicado automaticamente no .save Epic):
       - Qualidade de Render .... High Performance
       - Anti-Aliasing .......... Desligado
       - Detalhe de Render ...... Performance
       - Detalhe de Textura ..... High Performance
       - Detalhe de Mundo ....... High Performance
       - Detalhe de Particulas .. High Performance
       - Efeitos (Luz/Sombra/Clima/Desfoque) .... tudo Desligado
       - FPS Maximo ............. Uncapped
       - Escala 3D .............. 100% (sem borda preta ao usar High Performance)

  >> MODO CRIADOR (bonito + otimizado):
     Opcoes > Video:
       - Modo de Janela ........ Tela Cheia
       - Sincronizacao Vertical  Desligado
       - Qualidade de Render .... Alta Qualidade
       - Detalhe de Textura ..... Alta Qualidade   (texturas continuam nitidas)
       - Anti-Aliasing .......... FXAA (opcional, gravacao mais limpa)
       - Shaders de Alta Qualidade .. Ligado
       - Sombras Dinamicas ...... Desligado  (maior ganho de FPS; carro continua
                                   bonito. Se quiser sombra projetada nas
                                   gravacoes e tem GPU sobrando, veja o FAQ.)
       - Oclusao de Ambiente / Profundidade de Campo / Desfoque .. Desligado

Depois disso, e so jogar. Nao precisa repetir, a menos que troque de modo.


--------------------------------------------------------------------------------
 O QUE CADA MODO FAZ
--------------------------------------------------------------------------------

COMPLETO:
  - Texturas em 16x16 (LODBias 15), filtros em Point, anisotropico 0.
  - Sombras, AO, distorcao, fog, reflexos, folhas, fraturas, tessellation: OFF.
  - UncappedFramerate=True, bSmoothFrameRate=False, WaitForGPU=True.
  - Mantem o que importa pra ver a bola: decals dinamicos (marcas de pneu).
  -> Visual "batata", FPS no talo.

CRIADOR:
  - Texturas em alta (1024/512) com filtro Anisotropico 16 (nitido pra gravar).
  - Mantem: reflexos no carro, materiais HQ, folhas, fog, iluminacao do campo,
    HDR (FloatingPointRenderTargets), Apex Cloth (bandeiras/capas).
  - Remove o que pesa e pouco aparece: sombras dinamicas, AO, distorcao,
    radial blur, fraturas.
  - v22.3: mantem indicador da bola nas laterais/cantos (UnbatchedDecals +
    ForegroundShadowsOnWorld ligados; sombras dinamicas continuam OFF).
  - UncappedFramerate=True, WaitForGPU=True.
  -> Continua bonito pro publico, mas roda muito mais leve.

REMOVER:
  - Restaura o backup do seu .ini original (feito na 1a aplicacao), OU
  - Restaura o padrao de fabrica (stock) mantendo a sua resolucao, OU
  - Se nao houver backup, oferece apagar o .ini para o jogo criar um novo.
  - Sempre DESTRAVA o arquivo no final.


--------------------------------------------------------------------------------
 LAUNCH OPTIONS  (comando de inicializacao - Steam / Epic)
--------------------------------------------------------------------------------

No menu, a opcao [4] LAUNCH OPTIONS copia pro clipboard o melhor comando de
inicializacao (pesquisado e validado). Steam e Epic usam o MESMO comando:

      -nomovie -NOSPLASH -high

  STEAM: botao direito no RL > Propriedades > Geral > Opcoes de Inicializacao
  EPIC:  Biblioteca > 3 pontinhos no RL > Gerenciar >
         "Argumentos de linha de comando adicionais"
  (cole com Ctrl+V e confirme)

IMPORTANTE: no Rocket League as launch options quase NAO mudam FPS. O ganho real
vem do INI (que o GuttyRL aplica) + Opcoes > Video. As flags abaixo so ajudam no
tempo de boot e na prioridade do processo.

O que cada flag faz:
  -nomovie     pula os videos de intro       (boot mais rapido)
  -NOSPLASH    pula a tela de splash          (boot mais rapido)
  -high        prioridade Alta do processo    (TIRE se der stutter ou estalo de
                                               audio - acontece em alguns PCs)

Fora do comando - placebo/no-op no RL (a pesquisa confirmou):
  -NoVSync               o RL ignora; o V-Sync ja vai OFF pelo proprio INI.
  -nolog                 sem efeito util; ganho de FPS = zero.
  -NoSteamVR             no-op: o RL nao tem plugin de VR (nem na Steam).
  -no-stereo-rendering   placebo: o RL nao renderiza em estereo.
  -USEALLAVAILABLECORES  no RL e no-op (a engine ja gerencia as threads).
  -malloc=system         o RL (UE3) ignora; sem efeito.

Opcional (cole A MAO se quiser):
  -NoForceFeedback   MATA a vibracao do controle (so se voce nao quer rumble).

Todas seguras com o Easy Anti-Cheat (EAC), obrigatorio no online desde a Season
22. NAO use "Play without Easy Anti-Cheat" pra jogar ranqueada/online - essa
opcao e so pra treino offline / LAN.


--------------------------------------------------------------------------------
 BACKUPS E SEGURANCA
--------------------------------------------------------------------------------

  - Backup do seu original (1a vez):
      %USERPROFILE%\GuttyTECH\RL-Optimizer-v22\Backups\TASystemSettings.original.ini
  - Backup com data/hora antes de cada mudanca, na mesma pasta (*.bak).
  - Log de operacoes: ...\RL-Optimizer-v22\log.txt
  - A sua RESOLUCAO, modo de tela (Tela Cheia/Borderless) e auto-deteccao sao
    SEMPRE preservados ao aplicar qualquer modo - nada de tela preta.

  Este otimizador NAO usa: admin/UAC, bcdedit, mexidas de TCP/rede, takeown,
  nem PowerShell para aplicar - so comandos nativos. Por isso roda onde a
  versao antiga falhava.


--------------------------------------------------------------------------------
 ANTIVIRUS / "ACESSO CONTROLADO A PASTAS"
--------------------------------------------------------------------------------

Se aparecer "[X] Nao consigo gravar na pasta do jogo":
  - O Windows Defender tem "Acesso Controlado a Pastas" (protecao contra
    ransomware) que pode bloquear gravacao em Documentos.
  - Windows Defender > Protecao contra virus e ameacas > Gerenciar protecao
    contra ransomware > Acesso controlado a pastas > Permitir um app
    (libere o cmd.exe) OU desative temporariamente e rode de novo.
  - O mesmo vale se o jogo nao consegue salvar configuracoes.


--------------------------------------------------------------------------------
 ABRE E FECHA NA HORA?  (crash silencioso no startup)
--------------------------------------------------------------------------------

Se o GuttyTECH_RL.exe (ou GuttyRL.exe) abre e fecha imediatamente:

  1) EXTRAIA DO ZIP antes de rodar. Nao execute de dentro do arquivo compactado.

  2) ABRA O ROCKET LEAGUE 1 VEZ para criar o TASystemSettings.ini, depois rode
     o otimizador de novo. Sem o INI o app mostra erro e pede ENTER.

  3) ATUALIZE O WINDOWS (Configuracoes > Windows Update). Versoes antigas do
     Win10/11 podem crashar apps .NET 9 self-contained (fecha sem mensagem).

  4) LIBERE NO ANTIVIRUS / Acesso Controlado a Pastas:
     - O .exe single-file extrai arquivos temporarios na 1a execucao.
     - Bloqueio do %TEMP% ou do .exe = fecha na hora.
     - Libere o GuttyTECH_RL.exe nas excecoes do Defender/antivirus.

  5) PC ARM ou 32-bit: o .exe e so win-x64. Use Windows 64-bit (x64).

  6) LOG DE CRASH: %USERPROFILE%\GuttyTECH\RL-Optimizer-v22\crash.log
     Se existir, mande no Discord para suporte.

  Alternativa: use o GuttyRL.bat (modo pasta) se tiver a pasta templates junto.


--------------------------------------------------------------------------------
 POR QUE A v22 FUNCIONA ONDE A v21 FALHAVA
--------------------------------------------------------------------------------

  1. A v21 dependia de PowerShell (bloqueado por ExecutionPolicy/antivirus em
     varios PCs). A v22 aplica via copia de template nativa, sem PowerShell.
  2. A v21 usava 'bcdedit' (falha com Secure Boot/BitLocker, podia pedir chave
     de recuperacao). REMOVIDO.
  3. A v21 mexia em TCP/registro do sistema todo. REMOVIDO (o .ini sozinho
     entrega o ganho de FPS).
  4. A v21 forcava 1920x1080/Tela Cheia e depois trancava o arquivo -> tela
     preta/crash em monitores diferentes. A v22 PRESERVA a sua resolucao.
  5. O bug do PowerShell (usava $env:RL_TARGET sem nunca definir) -> nao
     aplicava nada. Eliminado (sem PowerShell).
  6. Backups iam pro %TEMP% (volatil). Agora ficam em %USERPROFILE%\GuttyTECH.

  Sobre o "somente-leitura": ele e NECESSARIO. Sem ele, o Rocket League
  sobrescreve o .ini a cada abertura e a otimizacao some. A v22 mantem o
  read-only (preservando sua resolucao pra nao crashar) e o REMOVER destrava.


--------------------------------------------------------------------------------
 FAQ
--------------------------------------------------------------------------------

P: Troquei de modo e quero voltar.
R: E so rodar de novo e escolher outro modo. Ele destrava, aplica e trava
   sozinho. Pra voltar ao original, use o REMOVER.

P: A sombra/marca da bola some nas laterais do campo (modo CRIADOR).
R: Reaplique o CRIADOR com a v22.3 ou mais nova. O template agora liga
   UnbatchedDecals e bEnableForegroundShadowsOnWorld (sem ligar sombras
   dinamicas). Se ainda sumir, confira World Detail = Alto no jogo.

P: Quero sombra dinamica no MODO CRIADOR (gravacao cinematografica).
R: Abra templates\INI_CRIADOR.txt, troque "DynamicShadows=False" por
   "DynamicShadows=True" (na secao [SystemSettings] e nas [SystemSettings
   ProfileDetail*]), salve e aplique o CRIADOR de novo.

P: O jogo atualizou. Preciso refazer?
R: So se o Rocket League resetar/recriar o TASystemSettings.ini. Nesse caso,
   rode o otimizador de novo e escolha o modo.

P: Mudei a resolucao no jogo e nao salvou.
R: O arquivo esta travado (somente-leitura). Rode o REMOVER, ajuste a
   resolucao no jogo, e aplique o modo de novo (ele vai preservar a nova res).

P: Posso editar os valores?
R: Sim. Os modos sao os arquivos em "templates\". Edite e rode o build_exe.bat
   de novo pra regerar o .exe (o .bat ja usa os .txt direto).

P: Apareceu "LIMITE DE CHAMADA ATINGIDO" ou "nao conectado ao Epic Online Services".
R: NAO e o otimizador nem o .ini (o .ini fica igual a uma config normal de FPS).
   E um limite TEMPORARIO da propria Epic, disparado quando o jogo faz muitas
   reconexoes em pouco tempo - tipico de quem fica abrindo/fechando o RL varias
   vezes seguidas testando. Solucao: feche o RL, espere 10-15 min, sincronize o
   relogio do Windows, e reabra UMA vez. Some sozinho. Se persistir: apague a pasta
   Documents\My Games\Rocket League\TAGame\Cache\ e libere o RL/Epic no antivirus.

================================================================================
  GUTTYTECH - TESSERACT v22.3
================================================================================
