# 💀 GUTTYTECH - ROCKET LEAGUE GUTTYTECH SCRIPT (V21)

![Version](https://img.shields.io/badge/Version-V21_Omega-red)
![Platform](https://img.shields.io/badge/Platform-Windows_10%20%7C%2011-blue)
![Engine](https://img.shields.io/badge/Target-Unreal_Engine_3-lightgrey)
![Status](https://img.shields.io/badge/Status-Stable_%2F_Extreme-success)

> **AVISO DE KERNEL:** Este não é um "tweak" comum de YouTube. O **RL_GUTTYTECH** altera o registro do Windows em Ring 0, desativa o Network Throttling, amordaça o HPET (High Precision Event Timer) e lobotomiza o arquivo nativo da Unreal Engine 3 (`TASystemSettings.ini`). O jogo deixará de ser um software de entretenimento e passará a ser um emulador de hitboxes com foco absoluto em Frametime Reto e DPC Latency nula.

---

### O Massacre dos FPS (Ganhos Extremos)
<img width="1214" height="262" alt="592042350-d74b9f01-9f18-45e6-ae97-3a54b063b759" src="https://github.com/user-attachments/assets/0803b876-9b40-4a1e-bac4-0f2eb8b6a961" />

## 📊 TELEMETRIA E BENCHMARKS (A Prova do Silício)
Testes auditados no **CapFrameX**. 
**Hardware Alvo:** Intel Core i9-12900KF | NVIDIA GeForce RTX 4090.

| Métrica               | Antes (Padrão)         | Depois          | Ganho real   |
| --------------------- | -------------- | -------------- | ---------- |
| **Média FPS**                | 608.72         | **800.41**       | **+31.5%** |
| **Mediana**                    | 629.33         | **833.82**       | **+32.5%** |
| **1% Low**                      | 354.57          | **443.52**       | **+25.1%** |
| **0.1% Low**                  | 240.99          | **262.11**         | **+8.8%**  |
| **Máximo**                    | 1,641.77        | **3,060.91**      | +86.4%  |
| **Adaptive STDEV**   | 22.74             | **24.91**           | +9.5%  |
| **CPU Max Thread**   | 78%               | 82%              | +4%    |

## 🔬 A AUTÓPSIA DO MOTOR (Por que o jogo original engasga?)
A Unreal Engine 3 (2006) não foi feita para hardware moderno. Por padrão, o jogo força *Texture Streaming*, coleta de lixo síncrona na CPU, limitadores de frames ocultos e buffers de renderização (`OneFrameThreadLag`) que causam micro-stutterings violentos (picos de até 150ms no frametime). 
Esse Script destrói essa arquitetura. Ele empurra o jogo para 16-pixels (Modo Batata absoluto para renderização interna), força o processamento Multi-Thread para arquiteturas Intel/AMD modernas e blinda o seu cabo USB contra o *ForceFeedback Polling*.

---

<img width="1234" height="524" alt="592042349-785c525e-6438-426a-bfda-a999bf6f06c0" src="https://github.com/user-attachments/assets/e39a0512-5a39-436e-9a53-126b2f0bde2f" />

| Métrica                         | Configuração A (Vermelho)            | Configuração B (Verde)                    |
| ------------------------------- | ------------------------------------ | ----------------------------------------- |
| **Moda (frametime mais comum)** | ~1,25 ms                             | ~1,70 ms                                  |
| **FPS equivalente na moda**     | ~800 FPS                             | ~588 FPS                                  |
| **Forma da distribuição**       | Leptocúrtica (pico agudo e estreito) | Platicúrtica (pico mais largo e achatado) |
| **Desvio padrão**               | Baixo (~0,18 ms)                     | Moderado (~0,35 ms)                       |
| **Comportamento da cauda**      | Queda rápida após 2,0 ms             | Cauda estendida até ~3,5 ms               |

---

## ⚙️ ARSENAL TÁTICO (O que o Script faz)

### 1. Otimização de Ring 0 (Windows & Rede)
*   **TCP/IP Hitreg Optimizer:** `TcpAckFrequency=1` e `TCPNoDelay=1`. O Algoritmo de Nagle é morto. Seus comandos chegam ao servidor no mesmo milissegundo do clique (Zero Ghost Hits).
*   **Morte do Network Throttling:** Remove o limite do Windows de 10 pacotes de rede por milissegundo.
*   **I.F.E.O. Injection:** O `RocketLeague.exe` é cravado eternamente em "High CPU Priority". O Windows nunca mais deixará processos secundários roubarem ciclos do jogo.
*   **Timers Amputados:** Desliga `useplatformclock` e `disabledynamictick`. Foco absoluto no processamento sem economia de energia.

### 2. Lobotomia da Unreal Engine 3 (`TASystemSettings.ini`)
*   **Renderização:** Geometria e texturas travadas em `MaxLODSize=16` (Modo Batata).
*   **Física APEX Morta:** Cálculos inúteis de CPU para demolições, fragmentos e antenas foram extraídos.
*   **Decoupling:** `WaitForGPU=False`. Sincronização entre CPU e placa de vídeo quebrada para FPS infinito.
*   **Bloqueio SID Universal (`*S-1-1-0`):** O script aplica um bloqueio nativo de leitura/escrita no arquivo `.ini`. O OneDrive, a Epic Games e a Steam são fisicamente impedidos de reverter as configurações.


### 🗄️ Registros Oficiais de Auditoria (Links CapFrameX)
Para provar que não há margem de erro, a telemetria bruta foi subida para os servidores oficiais do CapFrameX. Analise os dados com seus próprios olhos:
* 📉 **Sessão ANTES (Vanilla):** [Clique aqui para abrir o laudo original](https://www.capframex.com/sessioncollections/e7e2ab39-3c60-4bcb-8bcd-f6adebeba450)
* 🚀 **Sessão DEPOIS (GuttyTECH V21):** [Clique aqui para abrir o laudo otimizado] (https://www.capframex.com/sessioncollections/d03911ec-a7f5-4ae0-9134-9eee1c9b3766)

---

## 🛠️ COMO APLICAR O PROTOCOLO

### PASSO 1: Steam Launch Options (Obrigatório)
Antes de rodar o script, vá na sua Steam / Epic Games, clique com o botão direito no Rocket League > Propriedades > Opções de Inicialização (Launch Options). Cole **exatamente** esta linha:
```text
-nomovie -NOSPLASH -nolog -high -NoVSync -NoForceFeedback -no-stereo-rendering
```
*(Nota para teclado/mouse: Adicione `-NoController` no final da linha para zerar o USB Polling Rate).*

### PASSO 2: A Injeção de Código
1. Baixe o arquivo `RL_GuttyTECH_V21.bat` nos *Releases* deste repositório.
2. Certifique-se de que o Rocket League e o seu OneDrive estejam **FECHADOS**.
3. Clique com o botão direito no arquivo `.bat` e selecione **"Executar como Administrador"**.
4. O script fará a varredura (`Bloodhound Tracker`), encontrará a pasta, quebrará o selo, injetará a matemática e trancará o arquivo novamente.

### PASSO 3: O Reinício
Reinicie o seu computador imediatamente. As alterações de TCP/IP, HPET e Prioridade de Kernel exigem um reboot para serem aplicadas na placa-mãe.

---

## ⚠️ NOTA SOBRE A INTERFACE (Ghost UI)
Após aplicar o V21, **NÃO MEXA NAS OPÇÕES DE VÍDEO DENTRO DO JOGO**.
A Interface do Rocket League irá mentir para você. O menu pode mostrar caixas marcadas como "High Quality", mas o motor real do jogo, por trás dos panos, estará rodando na escuridão absoluta forçada pela GuttyTECH. Se você tentar mudar algo no menu, o jogo não conseguirá salvar (graças ao nosso selo SID de acesso negado). Entre no Freeplay e veja os gráficos quadrados e o FPS infinito com seus próprios olhos.

---
**Forged by GuttyTECH - Overclocker Specialist.**
*O limite do silício foi atingido.*
