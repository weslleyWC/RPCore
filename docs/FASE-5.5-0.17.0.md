# Fase 5.5 — RPCore 0.17.0: streamer

A 0.16.0 foi confirmada funcionando pelo usuário. Esta subentrega acrescenta transmissões locais de gameplay, com app Twitch do celular, chat contextual, audiência/seguidores, inscrições/bits/doações, pagamentos bancários e estatísticas persistentes. Arrancada é a próxima atividade da fase 5. Mantém a ordem solicitada: concluir fases principais, depois **fase 8: interface e mecânicas do HUD/menus** e **fase 9: refinamento de todos os trabalhos**, ambas com estudo aprofundado da PSRP.dll. Esta entrega não declara corrigidas as mecânicas de fome/sede relatadas.

## Instalação

Feche o GTA, extraia RPCore-update-0.17.0.zip e execute o instalador da pasta extraída com o seu caminho real:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

DLL pronta. Backup da DLL anterior e Jobs/Streamer.json acrescentado só se ausente. Saves, configurações, F2/celular, F11/teleporte e trabalhos/bolsas/estoques anteriores preservados. Não reinstala nem modifica OIV/YTD/RPF/DLC/dlclist.

## Como jogar e testar

1. **F2 → app Twitch**, novo ícone no último espaço da grade. Também pode abrir **F10 → Trabalhos / contratos → Streamer / transmissao**. Não exige comprar equipamento ou ir a um local.
2. Fora de live, ajuste canal e título se quiser. Canal: 3–20 letras/números/underscore; título: até 60 caracteres sem códigos de formatação/quebras de linha. Ficam salvos por perfil. Cancelar a entrada de texto preserva o anterior.
3. Escolha **Iniciar live** e feche o celular/menu. A duração padrão é **180 segundos de gameplay ativo**, em **seis blocos de 30 segundos**. Não há taxa de início.
4. Aparece o painel roxo original **TwitchOverlay** com audiência, seguidores/inscritos, receita desta live e chat. Andar, dirigir, nadar, chuva, ferimentos, procurado e trabalho ativo mudam as mensagens. O chat também pode comentar valores baixos de fome/sede existentes; isso não altera essas mecânicas.
5. **Pode transmitir enquanto trabalha.** A live não ocupa nem libera a trava dos trabalhos físicos. Pode iniciar antes de um trabalho ou durante um já ativo. Exemplo: inicie live, abra a central, contrate Post OP e faça o turno normalmente. Abrir menus pausa o relógio da live; fechá-los retoma. Não muda pagamento, requisitos ou encerramento do trabalho.
6. A cada bloco completo, o progresso e o pagamento entram no banco no mesmo salvamento. O chat mostra seguidores e o evento confirmado. Eventos: nova inscrição, bits, doação ou bloco normal com receita base. O histórico do celular recebe os créditos como informação secundária.
7. Ao completar seis blocos, a live encerra automaticamente e registra conclusão/estatísticas. Abra **Twitch → Estatisticas** ou o menu Streamer para conferir receita, seguidores, inscritos, bits, doações, tempo, pico e último resumo. Não há bônus extra de término.
8. Para parar antes, use **Encerrar live** no app ou no menu Streamer. Mantém todos os blocos pagos; fração de tempo ainda não confirmada não paga. **End continua reservado ao trabalho físico** e não encerra a live em paralelo.

## Regras de tempo e pagamento

O relógio só avança com personagem vivo em gameplay, fora de pausa, menus LemonUI, celular/radial/inventário modal e cutscenes. Longos saltos de frame/carregamento contam no máximo um segundo por atualização: não geram pagamentos retroativos. Morte, missão nativa ou personagem não humano solicitam encerramento. Procurado, dirigir e combate não são bloqueados pela transmissão; o streamer só observa o gameplay, sem alterar polícia/veículos/NPCs.

Padrão por bloco de 30 s:

| Evento | Chance por bloco | Receita adicional |
| --- | --- | --- |
| Inscrição | 10% | 7 |
| Bits | 20% | 10–50 bits, 1 por bit |
| Doação | 20% | 10–60 |
| Normal | 50% | Sem adicional |

Cada bloco tem **receita base 5**, configurável, mais o evento sorteado; ganha 1–3 seguidores por bloco. Exemplo: bloco normal paga 5; inscrição paga 12; 40 bits pagam 45. Uma live completa de seis blocos rende no mínimo 30 na configuração padrão, mais os adicionais. São valores próprios da economia do RPCore. Audiência é estimada por seguidores/contexto e limitada a 60.000, não espectadores reais. Não há conexão com Twitch, upload de vídeo, gravação nativa F1 ou dinheiro real.

Se falhar a gravação ou atingir limite de saldo/dados, o bloco fica pendente, com tempo congelado e **mesmo sorteio** para tentar novamente após cinco segundos. Não credita seguidores ou dinheiro antes de confirmar. Pode solicitar encerramento para descartar esse bloco ainda não confirmado; os anteriores ficam salvos. Se o encerramento não puder ser salvo, suspende novos ganhos e tenta novamente.

## Persistência, convivência e interface

Novo campo **Wallet.json/Streamer** por perfil: Channel, Title, Followers, Subscribers, Sessions, Completed, Blocks, Peak, Bits, Donations, Earned, Seconds, Last e recibos recentes (máximo 512). Início é um checkpoint sem custo. Cada bloco credita saldo, contadores e resumo no mesmo arquivo; falha restaura banco/referência anterior. Recibo confirmado não paga de novo após retry/reload. Resumo Last é da última sessão; totais continuam entre sessões. Canais/títulos editados fora de live.

Transmissão ativa não retoma após reload/troca de perfil; só tempo/blocos confirmados persistem. Resumo de sessão não fechada indica interrupção, sem prometer que continua ao vivo. A nova sessão substitui o resumo, preserva totais e campos desconhecidos. Saves/bolsas/licenças/itens/progresso dos outros sistemas não são consumidos nem convertidos.

O histórico de Phone.json é secundário: falha ao registrar notificação não desfaz nem repete o crédito em Wallet.json. As 11 posições anteriores do celular permanecem; Twitch é a 12ª. Os ícones e painel são dos YTD existentes, **PSRP_PHONE/TwitchIcon** e **TwitchOverlay**. O recorte alfa foi inspecionado no OIV: (21,233)–(346,661). O painel respeita escala/zona segura; fallback desenhado se a textura não estiver disponível. Oculta durante menus e pode ser escondido pelo app/menu sem desligar a live.

A alteração visual desta fase se restringe a ativar o app e apresentar a atividade nova. Refinamento global de F11, demais menus e das mecânicas ligadas ao HUD permanece na fase 8. Não muda a tecla F2, layout das outras telas ou a lógica de fome/sede. Sem câmera/prop global, gravação, conexão externa ou alteração da polícia. Conflitos de legibilidade com outros recursos devem ser reportados para teste/refinamento.

**Jobs/Streamer.json** contém duração, intervalo, receita base, valor de inscrição/bit, limites de doação e painel padrão. Duração deve ser múltipla do intervalo (ex.: 180/30); personalizações válidas são respeitadas. Configuração inválida fica no disco e usa defaults embutidos. Estado semanticamente inválido é preservado, sem zerar estatísticas.

## PSRP e diferenças deliberadas

Inspeção estática de PSRP/_JOB__LiveStreamer.cs e CustomPhone.cs: app inicia/encerra, edita canal/título, mostra estatísticas; chat/audiência reagem a contextos de gameplay e trabalhos; eventos de seguidores/inscritos/bits/doações creditam banco. Referência usa a textura TwitchOverlay e TwitchIcon fornecidas. Esses elementos orientaram uma implementação própria do RPCore.

Há diferenças: o RPCore usa seis blocos com checkpoint único, duração finita, receita base e tabela de eventos próprias; a referência tem contadores/probabilidades por tamanho do canal e mais contextos detalhados, como eventos específicos de cada emprego. A simulação inicial usa oito contextos gerais e leitura do trabalho ativo. O detalhamento/economia/realismo dos trabalhos, inclusive streamer, será revisto na fase 9; não é declarada paridade completa com a classe original. Não foi transplantado o código da referência.

## Arquivos alterados

- StreamerData.cs/StreamerJob.cs/config/Jobs/Streamer.json: canal/configuração, tempo elegível, sorteio estável, audiência/chat, eventos, checkpoints, resumo, interrupção e overlay.
- Economy.cs/Wallet.cs: campo Streamer e adaptadores bancários aditivos.
- Main.cs: 13ª entrada na central, com transmissão paralela às 12 atividades existentes; observa ShiftGate sem adquirir/liberar.
- PhoneSystem.cs/InterfaceData.cs: app Twitch/ações/histórico e 12º ícone, preservando os 11 anteriores; inscrição/remoção dos callbacks.
- AchievementSystem.cs: conquistas de bloco/live completa e cash_earned após crédito.
- Visuals.cs/config/Visuals/Bindings.json: banner de menu com fallback; TwitchOverlay/TwitchIcon vêm do catálogo existente.
- RPCore.csproj/tests/RPCore.Tests.csproj: configuração embutida e lógica real nos testes; tests/StreamerTests.cs: 47 casos novos. tests/InterfaceDataTests.cs: verifica os 12 ícones fornecidos.
- AssemblyInfo.cs/Main.cs/CoreMenu.cs/VisualAssetDefaults.cs: versão 0.17.0; scripts/audit-psrp-systems.py, relatórios, documentação e ZIPs atualizados.

## Validação e teste no GTA

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros/avisos. **614 testes aprovados**: 567 anteriores mais 47 novos. Cobrem canal/título, início/replay/rollback, distribuição/limites dos eventos/audiência, banco e estatísticas no mesmo checkpoint, ordem/último bloco, seis pagamentos/uma conclusão, pausa/carregamento, retry sem novo sorteio, encerramento, limits/unknowns e preservação das atividades/licença anteriores. Testes não executam GTA/renderização/entrada de celular.

1. Confira app Twitch no F2, edição persistente e atalhos anteriores.
2. Inicie e feche o celular; acompanhe seis blocos, eventos/chat e crédito bancário. Compare receita com o resumo; fim não pode pagar bônus extra.
3. Ande/dirija/trabalhe durante a live; confira contexto e que o trabalho continua com seu pagamento/cancelamento próprio. Segurar End encerra o trabalho, sem desligar a live.
4. Abra menus/pausa por mais de 30 s: não deve avançar nem pagar durante esse tempo. Oculte o painel e confirme que os blocos continuam em gameplay.
5. Encerre antes de 30 s e depois de um bloco; confira ausência de pagamento de fração e preservação do confirmado. Teste morte/reload/perfil e persistência sem pagamento duplicado.
6. Confira textura/chat/escala e compatibilidade com os recursos instalados; faça regressão rápida de telefone, inventário, trabalhos e teleporte.

O GTA não foi executado nesta nuvem. Fases 8 e 9 permanecem no roteiro, sem antecipar seus refinamentos.
