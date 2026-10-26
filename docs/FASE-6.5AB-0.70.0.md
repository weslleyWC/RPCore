# Fase6.5AB — jardinagem Mirror Park — 0.70.0

## Instalação

Baixe RPCore-update-0.70.0.zip da branch rpcore-entrega, extraia e execute Install-RPCore.ps1 **da pasta extraída**, com -GtaPath apontando à pasta real que contém GTA5.exe. Requer GTA V Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2 SHVDN3. Saves/configs, celularF2, teleportF11 e recursos .oiv/.rpf/YTD/props instalados são preservados. PSRP.dll foi referência estática, não é dependência para instalar junto.

## Roteiro do teste no GTA

1. Sua gangue precisa possuir o **território8**. F4 → Comercio dos territorios → seção da área8 → **GPS: cortador Mirror Park**.
2. A pé a até8m do início(1140.882,-671.9385,56.51244), seguro/sem procurado/combate/missão/cutscene e sem outra atividade ou contrato territorial pendente, escolha **Cortar grama /57 pontos**. Início gratuito.
3. Afaste-se pelo menos3m da vaga e deixe-a livre de carros/pedestres. O cortador mower próprio aparece com blip verde. Nenhum veículo existente é apagado para abrir espaço.
4. Entre como motorista. Passe devagar **sobre os pontos de grama**: raio horizontal1m do centro do cortador, diferença vertical máxima3m e velocidade até8m/s(28,8km/h). Somente o cortador fornecido conta; carro pessoal, passageiro ou jogador a pé não cortam. Não é preciso apertarE em cada ponto.
5. GPS aponta à grama restante mais próxima, com seta verde. Cada ponto confirmado é salvo e seu prop próprio removido; o contador mostra progresso. Limpe todos os **57 pontos**.
6. Pare, saia e pressione **E a até4m do cortador vazio/parado** para confirmar o contrato. Também pode confirmarE a pé no ponto inicial. Não exige dirigir de volta ao início após cortar tudo.
7. Confira **Jardinagem /bonus2X** no F4: processamento automático na área8 fica2X por60minutos reais. Não liga automático sozinho, não paga dinheiro pessoal nem cria receita suja. Com receita e automático ativos, lote padrão até$500 passa de60s para aproximadamente30s ativos;85% líquido ao caixa e15% taxa permanecem. Bônus não se multiplicam; não há produção offline.

## Suspender, retomar, cancelar

**END suspende**. Cortes salvos permanecem. F4 mostra pendência, GPS, retomada e cancelamento.

- Saia do cortador anterior e deixe-o parado/livre para liberação. Ele não é apagado enquanto ocupado, em movimento ou conectado. Retomada/início aguardam a liberação do anterior, evitando duplicar cortadores. Volte **a pé ao ponto inicial**, com posse/segurança e retome pelo F4.
- Retomada parcial recria somente a grama ainda não cortada e um novo cortador próprio, mantendo índices e opções do contrato. Não refaz pontos salvos.
- Com os57 pontos já salvos, retome a pé no ponto inicial e confirmeE. Não precisa recriar veículo/grama nem repetir cortes.
- Cancelamento gratuito remove só a pendência e não concede prêmio parcial. Pode cancelar sem posse/filiação em segurança; bônus de contrato anterior mantém vencimento.
- Save falhou: corte mantém prova anterior e grama para nova tentativa; conclusão mantém pendência pronta e bônus anterior. Não há prêmio duplicado.

Morte/procurado/combate/ragdoll/missão/cutscene/perda de posse/filiação/perfil/ator, afastamento300m do início, perda do cortador ou separação150m suspendem. Pausa/menus não cortam nem avançam preparo. Pendência suspensa bloqueia outras operações econômicas territoriais até concluir/cancelar. Outras atividades podem usar a trava global depois da suspensão.

## Arquitetura e compatibilidade

GangMowingData.cs: opções/defaults, provas de corte, contas, validação geométrica e transações. GangMowing.cs: runtime/menus/GPS/props/cortador/limpeza. Config **Gangs/Mowing.json** preserva57 pontos/headings, mower e hash de grama992644101 da PSRP. Opções BoostMinutes60(1…180), StartRadius8(3…15), MowRadius1(0,5…2), MaxHeight3(1…4), MaxSpeed8(1…12);1…200 pontos distintos a até250m do início. Alterações afetam novos contratos; snapshot atual permanece. Config inválida usa defaults em memória sem substituir original.

Wallet.GangMowing opcional/extensível: Pending Id/Gang/Area8/Options/Mowed(lista de índices únicos); Accounts Gang/Area8/Completed/StartedUtcTicks/ExpiresUtcTicks; até640 contas/256 recibos. Sem handles, modelos carregados ou timers nativos persistidos. Consultar legado não cria livro/bônus. Campos futuros preservados.

Begin/Cut/Cancel/Complete clonam o livro e gravam uma única Wallet com rollback, replay, posse, índices, capacidade e overflow protegidos. Prova final requer todos os pontos distintos; remoção da pendência e bônus são atômicos. Não altera caixa pessoal/banco, inventário, estoque, receita suja, tesouraria, reputação/diplomacia ou progressão. Nova conclusão real renova janela a partir daquele UTC, sem somar duração; replay não renova. Relógio anterior à concessão não ativa; expiração inclui pausa/jogo fechado. Oito multiplicadores usam máximo2X no mesmoUTC.

Modelos assíncronos com limite10s; spawn em vaga livre sem apagar outros recursos; props em lotes de8/tick e sem colisão, marcados DECOR próprio. Respeita Visuals.Enabled/UseProps: sem props cosméticos, os57 pontos, markers, veículo e prova geométrica continuam obrigatórios. Com props ativos, perda de prop próprio suspende sem perder cortes. Não remove grama/props existentes do mapa ou de outro mod, só os próprios. Cortador reservado no serviço compartilhado JobVehicles; limpeza adiada segue em ticks inativos. Fechamento do script libera naturalmente veículos protegidos, sem ejetar/teleportar jogador, alterar polícia/relações globais ou encerrar scripts do GTA.

## Comparação e limitações

PSRP/GangSpawner.cs StartMirrorParkMowing/UpdateMirrorParkMowing/CompleteMirrorParkMowing conferidos: território8,57 coordenadas/headings, mower/início/rumo102,5347, grama992644101, jogador motorista do cortador específico, raioXY1m, GPS próximo a cada1,2s e2XUTC1h. Código próprio.

Diferenças deliberadas: persistência por ponto e retomada parcial; props opcionais; limites de altura/velocidade; confirmação finalE; END suspende. Limpeza não repete saída forçada/teleporte/exclusão de veículo ocupado da referência. Sem MissionWin em outro arquivo. HUD nativo simples; fases8(interface/necessidades) e9(realismo dos trabalhos) continuam futuras.

**GTA não executado nesta nuvem.** Teste render/posição das57 gramas, acesso/cortador, raio de corte, GPS, contagem, pausa, END/reload parcial/pronto, cancelamento, posse/perfil, veículo ocupado e convivência com trabalhos/bônus anteriores.2545 testes aprovados(2510 anteriores +35 novos); build Release net48 sem erros/avisos.
