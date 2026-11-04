GUTTYTECH RL Optimizer v25.0.25

Desculpa. O bug dos presets, da cor do carro e do botão de voltar foi erro do app.

O que acontecia:
O app regravava o save inteiro para mudar o menu de vídeo, e o formato saía errado. O jogo descartava o bloco e voltava para o padrão.
- O modo não ficava aplicado. O menu voltava para Alta qualidade / 60 FPS e, na sessão seguinte, o jogo reescrevia o INI.
- O preset perdia o vínculo. Na partida, o carro entrava com a cor do time.
- O REMOVER só devolvia o INI. O menu antigo continuava dentro do save, então parecia que voltar não funcionava.

A v25.0.24 parou de regravar a garagem, mas o menu dessas contas também parou de sincronizar. A causa de verdade era o tamanho dos campos, não só o tipo. Isso está corrigido agora.

O que a v25.0.25 faz:
- Muda só o bloco de vídeo, gameplay e câmera, no formato que o próprio jogo grava. Garagem, preset, tinta e inventário ficam byte a byte iguais, conferidos antes e depois de gravar.
- Conta com garagem grande volta a sincronizar o menu. O save da Steam também, que antes nunca sincronizava.
- Save leve que uma versão antiga já tinha regravado é consertado no próximo apply.
- REMOVER devolve o menu de vídeo do save. Câmera e FOV que você mudou depois ficam. Quem já tinha aplicado numa versão anterior fica no padrão do jogo.
- RESTAURAR PRESETS também repõe os saves _1, _2 e _3 quebrados, grava com a data de agora para a nuvem não passar por cima, não cobre um save bom com backup velho e não mistura conta Epic com Steam.
- O backup para de copiar tudo de novo a cada vez que o jogo fecha.

Se o preset já sumiu:
1. Feche o Rocket League
2. Atualize o app
3. Clique em RESTAURAR PRESETS, com o jogo fechado
4. Abra o jogo e confira a garagem antes de ligar a nuvem
5. Aplique COMPLETO ou CRIADOR de novo

Se não sobrou nenhuma cópia boa do save, o item da conta continua. O preset local pode precisar ser montado de novo.

Como atualizar:
1. Feche o GuttyTECH_RL que estiver aberto
2. Baixe o GuttyTECH_RL.exe novo, ou deixe o app atualizar
3. Abra o .exe novo
4. Rode o modo de novo, com o jogo fechado

Roda como administrador. Só trocar o arquivo.
