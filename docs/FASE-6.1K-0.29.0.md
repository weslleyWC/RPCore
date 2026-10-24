# RPCore 0.29.0 — fase 6.1K: câmeras e monitoramento PDM

## Instalação

Extraia RPCore-update-0.29.0.zip e abra o PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Ajuste o caminho se necessário. Requer GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não instale PSRP.dll como segundo script; é referência de análise. Não precisa reinstalar .oiv/.rpf. Backup da DLL anterior, saves/configurações anteriores conservados e Jobs/PdmSurveillance.json instalado se ausente. Celular F2 e teleporte F11 preservados.

## O que mudou e como usar

F10 → Trabalhos → PDM → Cameras / monitoramento. Adquira os equipamentos por $15000 do caixa da empresa, perto do escritório externo e com empresa adquirida. Compra única, sem mensalidade, indenização ou bônus em vendas. Entra nas despesas de melhorias da empresa e não cobra do banco pessoal. Atualizar não compra automaticamente nem reinicia a empresa.

Depois, no mesmo menu, escolha Ver: Fachada / entrada, Lateral / patio ou Retirada / acesso. Fique a pé até cinco metros do escritório externo, sem combate/procurado/missão e sem atividade física concorrente. Setas esquerda/direita trocam a câmera de forma circular. Backspace, Esc ou End saem. F2/F11/J fecham o monitor; pressione novamente para abrir celular/configurações/radial. Limite padrão de dois minutos por sessão; reabra para continuar.

A imagem é ao vivo do mundo do GTA. Você permanece no escritório, sem teleporte, congelamento ou invulnerabilidade. Movimentação e ataques ficam bloqueados somente enquanto o monitor está ativo. Pausa, dano/perda de armadura, combate, procurado, missão/cutscene, morte, distância ou câmera indisponível encerram a sessão. A atividade é liberada ao sair. Não há gravação/reprodução, acesso remoto por celular nem ataque gerado para testar câmera.

Três props prop_cctv_cam_04c nos pontos/rotações da PSRP:

| Câmera | X | Y | Z | Rotação Z |
| --- | --- | --- | --- | --- |
| Fachada | -39.0766 | -1119.456 | 29.3445 | -108.5868 |
| Lateral | -56.6277 | -1081.885 | 29.4098 | -111.5288 |
| Retirada | -23.2955 | -1086.04 | 28.4994 | 155.1891 |

Equipamento equivalente já existente perto do ponto é preservado e utilizado, sem duplicação deliberada. Só props criados pelo RPCore são recolhidos ao afastar/trocar perfil/encerrar. Falha ao carregar modelo conserva a compra e aguarda nova tentativa. Não altera os arquivos OpenIV.

## Roteiro de teste no GTA

1. Com empresa e pelo menos $15000 no caixa, compre Cameras / monitoramento. Confirme débito único e registro em melhorias/livro da empresa. Reabra/reinicie: deve constar adquirido, sem nova cobrança e com mesmo estoque/equipe. Banco pessoal não deve mudar.
2. Observe os três equipamentos nas posições indicadas. Confira ausência de duplicação nos pontos que já possuam o mesmo prop. Afaste-se e volte: somente representação própria é recolhida/recriada; compra continua salva.
3. No escritório, escolha cada canal e use as setas nas duas direções, inclusive do terceiro ao primeiro. Confira imagem real, nome correto, visão desobstruída e orientação útil da área. Enquadramento é próprio e depende dos mapas/interiores instalados; reporte canal com parede/objeto na frente.
4. Saia por Backspace/Esc/End, repita dez vezes e confira câmera normal, movimento/ataque e menu funcionando. Use F2/F11/J durante a visão: fecha o monitor; aperte de novo e confirme aplicativo/menu habitual. Aguarde dois minutos e confira saída automática.
5. Teste pausa, morte, início de missão/cutscene, troca de perfil e dano recebido enquanto observa. Deve fechar sem câmera/controles presos, sem imunidade e sem mudar dinheiro. Não precisa provocar procurado para usar o monitor; com procurado ele deve recusar/sair.
6. Tente ver enquanto outra atividade física está ativa: deve pedir encerramento da atividade. Caso use um mod de câmera, inicie o monitor só quando ele devolver a visão normal; o RPCore recusa entrar sobre câmera já renderizada por outro sistema e abandona a sessão se outra câmera assumir. Confirme retorno adequado com seus mods instalados.
7. Confira F2, F11/teleporte, radial J, inventário, showroom, mecânico/zeladoria/sujeira, segurança/salários, vendas, financiamentos, porto, carro-forte, retomada, locadora e garagem. Save antigo sem câmeras deve continuar não adquirido e sem cobrança.

## Configuração e referência

Jobs/PdmSurveillance.json: Price, Model, FieldOfView (40–90), MaxViewSeconds (30–300) e Cameras (1–6), com Mount, View e LookAt. Mount define o prop e Heading sua rotação; View/LookAt definem o enquadramento próprio, com View próximo ao equipamento. Configuração inválida usa defaults em memória conservando original. Alterar preço depois de comprar não modifica o valor já pago.

Analisados PurchasePDMCamera, SpawnPDMCamerasOnLoad, camSpawnpos e o preview de compra da PSRP.dll. Preço/modelo/montagens são da referência; código, enquadramentos e monitor com múltiplos canais são próprios. A referência usa remoção por proximidade e relocação do personagem no preview; o RPCore conserva objetos externos e não reloca o jogador.

Não se declara gravação, roubo/seguro, benefício econômico de vigilância ou toda empresa PDM concluídos. Fases 8/9 continuam reservadas ao refinamento combinado. Build e testes em VALIDACAO.md; comparação em PSRP-SYSTEMS-AUDIT-0.29.0.json. GTA não executado nesta nuvem.
