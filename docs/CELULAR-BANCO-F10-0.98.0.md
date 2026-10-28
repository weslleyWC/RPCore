# Refinamento0.98 — celular F2, banco e F10

Referência lida: CustomPhone (aplicativos, listas/detalhes de mensagens/notificações, leitura, notas, relógio/cronômetro/temporizador) e MoneyTerminal (depósito/saque com valor escolhido), além da organização de personagens em SettingsMenu. Implementação própria sobre a arquitetura atual; nenhuma DLL de referência foi executada no GTA neste ambiente.

## Celular

F2 continua sendo o atalho configurável; os12 aplicativos, moldura, wallpapers e ícones instalados são mantidos. Mouse agora seleciona/abre aplicativos e linhas, roda rola listas/textos e botão direito volta. Pressionar e soltar deve ocorrer no mesmo alvo; abrir/trocar dispositivo com botão mantido não confirma. Teclado continua setas/Enter/Backspace; controle usa direcional/A/B. As teclas de armas são bloqueadas por frame durante a tela mesmo com a roda nativa habilitada. Não muda movimento/vida/posição globalmente.

O aparelho continua indisponível dirigindo em movimento ou como passageiro, em missão, combate, pausa/cutscene, consumo, morte ou estados inseguros. Troca de ped/perfil fecha. Ações externas são enfileiradas para sair do modal e revalidar ator/perfil/controle/prazo, sem executar um atalho por baixo de outro menu.

Os serviços do celular chamam o sistema correspondente diretamente: Banco abre o banco do F10, Garagens chama Garages, Configurações abre o F11. Corrige o caso GarageKey=F11 abrir Settings por precedência do dispatcher; suas teclas não são regravadas. Serviços indisponíveis/falhados dão aviso. Uber/Twitch mantêm as mesmas ações e pagamentos existentes, sem revisão do trabalho nesta entrega.

## Histórico, notas e aparência

As64 entradas locais podem ser percorridas, em vez de mostrar somente as cinco primeiras. Enter abre o texto em páginas e marca somente aquela entrada como lida; navegar até a lista não apaga todos os avisos. Setas/roda percorrem o texto completo. Badges de mensagens e notificações refletem as respectivas entradas não lidas. Eventos Uber/Twitch entram como mensagem local, consumo/necessidades como notificação. Entradas antigas sem Kind continuam disponíveis em ambas as listas; a leitura da mesma entrada reflete nas duas.

Notas têm lista, visualização completa em páginas, edição e exclusão confirmada com Cancelar selecionado inicialmente. Delete na lista abre a confirmação; não apaga imediatamente. Nota alterada desde a seleção não pode ser removida por uma confirmação antiga. Até30 notas e120 caracteres em nota nova/editada; textos legados maiores são conservados e podem ser lidos sem truncar o save. Previews podem ser abreviados, sem cortar o texto salvo.

Notas, leitura, histórico e wallpaper são gravados numa cópia candidata de Phone.json antes de trocar o estado em memória. Erro normal mantém dados anteriores, não derruba o telefone nem cancela consumo/pagamento que originou um evento. Campos desconhecidos, notas antigas, wallpaper e histórico continuam no formato existente; Kind é opcional. Listas inválidas/nulas/maiores que os limites não são filtradas silenciosamente e regravadas: o original é conservado e a edição bloqueada com diagnóstico. O histórico continua limitado a64 ao acrescentar um evento novo, como antes; adicionar evento descarta apenas o mais antigo que excederia esse limite.

A visualização usa PhoneOptionsBG, NotesOverlay, FitnessOverlay e TwitchOverlay quando disponíveis, com fallback para retângulo. Fitness mostra saúde percentual, colete, necessidades e fôlego dos mesmos dados do HUD. Calculadora continua local, com dois operandos e um operador, sem eval/rede; não foi anunciada como teclado completo da referência.

## Relógio

Clock abre Cronômetro e Temporizador. O cronômetro permite iniciar, pausar, continuar e zerar. O temporizador aceita1–1440 minutos, pausa/continua/zára e avisa uma vez ao concluir, inclusive com o celular fechado. Tempo avança por deltas válidos de frames ativos; pausa/missão/stall não criam crédito retroativo e não há avanço offline. Os dois são de sessão: reiniciar plugins/trocar perfil zera, sem transferir tempo entre personagens. Formato suporta horas acima de24 sem voltar a00.

## Objeto do telefone

Apresentador continua separado dos consumíveis. O telefone recebe decorador RPCorePhone com handle/modelo/token; ao fechar/interromper, desanexa e remove apenas a entidade própria, verifica se sumiu e repete se necessário. Reload procura somente objetos próprios identificados. Poses/modelos configurados são copiados, transforms inválidos voltam a defaults de telefone, e não são removidos objetos por modelo/proximidade. Limite de sobras impede continuar criando objetos quando a limpeza não consegue avançar. Só para a animação iniciada pelo apresentador e só libera o dicionário de animação que solicitou; não usa ClearTasks. Ped armado/em veículo pode usar a interface onde permitido, sem prop/pose que substitua sua arma ou controle.

## F10 e banco

Quatro grupos principais permanecem: serviços/trabalho, banco, personagens/perfis, avançado/manutenção. Estados de submenus são atualizados preservando seleção; erro de uma ação é registrado/avisado sem derrubar todo o F10. Contexto inseguro fecha menus próprios.

Banco mostra banco/carteira atuais, depósito/saque por valor inteiro escolhido, máximo que cabe no destino e atalhos de$100 anteriores. Saldo, capacidade e overflow são revalidados; a operação usa o BankTransfer existente, com gravação e reversão de falha normal. Histórico de conquistas não repete o dinheiro nem desfaz uma transferência já confirmada se seu registro falhar. Banco/carteira nativa e arquivo JSON não formam uma transação de crash entre repositórios; não há garantia nova para queda do processo entre essas etapas. Não foram adicionados empréstimos/juros/renda da PSRP.

Importações e perfis legados ficam em submenus próprios. Saves duplicados com o mesmo alvo não geram linhas repetidas; perfil já importado não é sobrescrito/oferecido como novo. Importar e usar perfil exigem confirmação com Cancelar primeiro e conferência de ator/perfil. Usar dados legados exige realmente mp_m_freemode_01 ou mp_f_freemode_01, sem mudar ped/aparência/posição. Criar/carregar personagens modernos continua no sistema existente. A conversão e complementos do importador anterior são preservados; a entrega revisa fluxo/confirmação, não declara todos os campos da PSRP convertidos.

## Validação GTA

3772 testes aprovados (3725 anteriores +47), build Release net48 sem erros/avisos. Testes verificam dados/rolagem/hits, gravação candidata/falhas/metadados, notas/confirmadores antigos, histórico64/leitura individual/classificação, relógios sem crédito offline, cálculo bancário e cópia/fallback de poses. Natives, GUI e mundo exigem este teste:

1. F2: clique nos12 apps, navegue por mouse/roda/teclado/controle; teste troca de dispositivo com botão mantido e soltar fora. Confirme F2/J/F11/teleporte após sair.
2. Notas: criar, ler, editar, cancelar exclusão, confirmar e recarregar. Wallpaper também deve persistir; cancelar teclado não altera dados.
3. Histórico: navegue além da quinta entrada, abra uma e confira que as outras continuam não lidas. Leia texto longo com setas/roda, volte e confira badges.
4. Clock: iniciar/pausar/continuar/zerar cronômetro. Timer1 minuto com celular aberto/fechado, pausa de jogo e reload; deve avisar uma vez, sem descontar tempo offline.
5. Serviços: Banco abre banco diretamente; Garagens abre Garages mesmo com GarageKey=F11, Settings abre F11. Faça regressão de ações Uber/Twitch, contratos, rádio, bolsa e inventário.
6. F10: depositar/sacar valor escolhido, máximo e$100. Confira dinheiro/banco antes/depois, insuficiência/limite, seleção de submenu, fechar/reabrir/recarregar. Não deve dar ou remover dinheiro duas vezes.
7. Perfis: cancelar importação/troca não faz nada. Confira acesso freemode, perfis já existentes e originais PSRP conservados. Operações bancárias devem permanecer no personagem correto.
8. Telefone na mão: fechar/abrir/interromper/pausar/entrar em carro/morrer/recarregar. Objeto deve sair, consumíveis continuam com um único prop e outros mods preservados. Teste UI/fontes/overlays na sua resolução/zona segura.
9. Regressão0.94–0.97: arraste/peso/munição, itens no chão, compras em lote, uso pelo radial, GPS/marcadores e sono. Outros sistemas continuam no escopo em COBERTURA-REFINAMENTO.md.
