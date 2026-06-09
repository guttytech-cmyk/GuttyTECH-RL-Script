<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&color=0A0A0A&height=120&section=header&text=ROCKET%20LEAGUE%20GUTTYTECH&fontSize=36&fontColor=E50A0A&animation=fadeIn" alt="RL GuttyTECH" />

[![Version](https://img.shields.io/badge/Version-V21_Omega-E50A0A?style=for-the-badge)](https://guttytech.com)
[![Platform](https://img.shields.io/badge/Platform-Windows_10%20%7C%2011-0078D4?style=for-the-badge&logo=windows)](https://guttytech.com)
[![Engine](https://img.shields.io/badge/Engine-Unreal_Engine_3-121212?style=for-the-badge)](https://github.com/guttytech-cmyk/GuttyTECH-RL-Script)
[![Status](https://img.shields.io/badge/Status-Stable_Extreme-00C853?style=for-the-badge)](https://guttytech.com)

**Ring-0 registry · UE3 TASystemSettings · HPET kill · Network throttle off**

[Website](https://guttytech.com) · [Commander](https://github.com/guttytech-cmyk/Commander) · [Contato](mailto:admin@guttytech.com)

</div>

---

> **AVISO KERNEL:** O `RL_GUTTYTECH` altera registro em Ring 0, desativa Network Throttling, remove HPET e ajusta `TASystemSettings.ini` da Unreal Engine 3. Foco absoluto em frametime reto e DPC latency minima.

---

## Benchmarks — CapFrameX

**Hardware:** Intel Core i9-12900KF · NVIDIA GeForce RTX 4090

| Metrica | Antes | Depois | Ganho |
|---------|------:|-------:|------:|
| Media FPS | 608.72 | **800.41** | **+31.5%** |
| Mediana | 629.33 | **833.82** | **+32.5%** |
| 1% Low | 354.57 | **443.52** | **+25.1%** |
| 0.1% Low | 240.99 | **262.11** | **+8.8%** |
| Maximo | 1,641.77 | **3,060.91** | +86.4% |

<div align="center">

![Bar Charts](bar_charts.png)
![Frame Time](frame_time.png)

</div>

---

## O que o script faz

| Camada | Acao |
|--------|------|
| **UE3** | `AllowPerFrameSleep=False`, `OneFrameThreadLag=False`, `bSmoothFrameRate=False` |
| **Timer** | HPET / Dynamic Tick desativados via registro |
| **Rede** | Nagle, RSC, throttling de rede neutralizados |
| **GPU** | Prioridade de fila e latencia reduzida no driver path |

---

## Uso

```batch
:: Executar como Administrador
RL_GUTTYTECH_v21.5.bat
```

1. Feche o Rocket League e o Epic/Steam
2. Execute o `.bat` como **Administrador**
3. Reinicie o PC antes da primeira sessao pos-tweak

---

## Por que a UE3 engasga (padrao)

A Unreal Engine 3 forca texture streaming, GC sincrono, limitadores de frame e `OneFrameThreadLag` — micro-stutters de ate **150ms** no frametime. Este payload remove esses gargalos no nivel de config e registro.

---

<div align="center">

[![GuttyTECH](https://img.shields.io/badge/GuttyTECH-guttytech.com-E50A0A?style=for-the-badge)](https://guttytech.com)
[![Suite completa](https://img.shields.io/badge/Arsenal-Commander-121212?style=for-the-badge)](https://github.com/guttytech-cmyk/Commander)

<img src="https://capsule-render.vercel.app/api?type=waving&color=E50A0A&height=70&section=footer&fontSize=14&fontColor=0A0A0A" alt="footer" />

</div>
