# 0.85.0 — Halloween / The Last Ride

Acesso: **F11 → Halloween / The Last Ride**, menu centralF10 ou marcador de fantasma no mapa. O menu oferece GPS para o ponto original:232.1685,-1389.492,29.99907. Disponível para teste durante todo o ano; nada inicia automaticamente em saves antigos. Configuração nova`Missions/Halloween.json`; registro opcionalWallet.Halloween.

## Roteiro de teste

1. Instale com GTA fechado, como as versões anteriores. Abra F11 e marque **GPS: The Last Ride**. Vá ao ponto a pé, até3,5m, sem procurado/missão/trabalho/atividade. E perto do marcador abre o mesmo menu.
2. Escolha **Sustos / imagem e áudio** antes de começar. Ligado usa`PSRP_Banners / JumpScareOne`, do YTD já instalado, e`RPCore/Audio/Seasonal/Halloween1.wav`, já fornecido. A imagem aparece rapidamente com áudio: pode desligar esta opção antes do teste. Não muda clima, horário nem câmera globais; pode jogar à noite para a atmosfera. Sem textura/áudio, a corrida continua com aviso visual simples.
3. **Iniciar The Last Ride** cria umRomero próprio na vaga original. Vá até ele e assuma o volante. Há120s para entrar; os180s de corrida começam apenas quando você é o motorista real. Não teleporta o jogador nem força entrada. Vaga/piso tem espera até5s, modelos até10s; indisponibilidade interrompe sem apagar ambiente.
4. Siga a rota até o cemitério. Carros fantasmas próprios com motoristas invisíveis/desarmados perseguem e tentam colidir. Padrão: até4 ativos e12 criados ao longo da tentativa, novas tentativas de onda a cada10s(ou5s se não houver vaga). Spawns exigem rua livre, piso/altura e ponto fora da câmera; tráfego congestionado pode reduzir as ondas.
5. **E** no teclado ou **polegar direito** no controle aciona pulso defensivo, pronto inicialmente e recarregando em8s de mundo ativo. Alcance12m, queda de força e anel visual650ms. Só afasta perseguidores desta missão, com motorista próprio vivo e sem ocupante externo/anexo. Carro pessoal/trânsito/veículos de outros mods não recebem impulso direto. E não precisa ficar pressionado.
6. Chegue ao destino em menos180s e **pare até5m**, ao volante doRomero próprio; entrega automática. Veja tempo, distância, integridade e pulso no painel. A conclusão é registrada uma vez, **sem pagamento em dinheiro**, conforme aPSRP. Polícia nativa não é apagada, nem ao terminar.
7. Teste também sair do volante depois de iniciar, destruir/perderRomero e deixar o prazo acabar: reprova. Morte/missão nativa/troca de perfil/recurso anexado são tratados com falha ou interrupção apropriada. **END** ou encerrar no menu registra interrupção. Nenhuma operação da missão debita ou credita banco/dinheiro/inventário.
8. Recarregue durante a corrida: último registro vira interrompido, sem recriar perseguidores/Romero reparado, entregar automaticamente ou conceder resultado. Nova tentativa começa do início no marcador. Preparações/recebimentos do assalto ao estúdio e demais saves ficam mantidos.
9. Confira limpeza ao terminar dirigindo oRomero, em movimento, com ocupantes externos e com perseguidores destruídos. Veículos próprios ocupados/em movimento/no ar/anexados não são apagados; saia quando seguro para permitir recolhimento. Motoristas próprios voltam a ficar visíveis; não são modificados motoristas do ambiente.

## Cronômetro e apresentação

Esta é uma corrida cronometrada enquanto o mundo está rodando. **F11/LemonUI/celular não pausam o GTA**, então cronômetro e perseguidores continuam; não há congelamento artificial de carros/NPCs para navegar menus. **ESC/pausa real do GTA pausa** o cronômetro. Travamentos maiores que1s não recuperam tempo em bloco. HUD/sustos só desenham com interface livre; sustos usam primeiro intervalo30–75s e depois45–75s.

Estatísticas: iniciadas/concluídas/falhas/interrompidas/pulsos, histórico32, tempo e condição final. Checkpoints a cada5s, ao sair/salvar e antes do efeito do pulso. Falha de gravação não aplica impulso nem deixa contagem parcial; repetição de gravação/resultado não duplica estatística. Não há simulação offline ou retomada de um carro parcialmente destruído. Configuração é capturada ao aceitar: edição posterior não muda tentativa existente. Config inválida fica no disco e usa defaults em memória; journal inválido é protegido.

## Comparação e validação

Referência:HalloweenHearseMission inteira668linhas. Mesmos pontos/rumo, Romero,180s, raio inicial3,5m/destino5m, pulso8s/12m/27, retask1,5s, ram2,8s/36m e recursos gráficos/áudio originais. Diferenças deliberadas: perseguição finita própria em vez de tomar controle dos motoristas ambiente205m; pulso restrito a recursos próprios; jogador precisa dirigir/parar em vez de chegar como passageiro; sem forçar23h/tempestade ou câmera; menu inicial funcional; sustos opcionais; nenhuma recompensa inventada.

3193 testes aprovados, Release net48 sem erros/avisos. Sem GTA neste ambiente: rota/dificuldade180s, piso/modelos, perseguidores/colisões/pulso, controle, YTD/áudio reais, ocupantes/limpeza e coexistência precisam deste teste. OIV/RPF/YTD/áudios e atalhosF2/F11 anteriores mantidos. Próximo bloco: editor persistente; depois revisão final. Refinamentos8/9 continuam reservados.
