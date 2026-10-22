# RPCore 0.5.0 — composição dos prints

O Downloads.zip contém três imagens de referência, preservadas em referencias-0.5.0 com hashes. Elas mostram o resultado desejado, não uma execução desta DLL. O ZIP não inclui uma captura nova do RPCore atual/radial; para radial e F11 foram usadas as referências anteriores e a inspeção da PSRP.dll. O ambiente de desenvolvimento não executa GTA/Windows/OpenIV.

## Atualizar

Feche o GTA, extraia RPCore-update-0.5.0.zip fora do jogo e execute Install-RPCore.ps1 apontando para a pasta do GTA. É a DLL pronta, não exige compilação. O instalador mantém backup e não sobrescreve saves, INIs ou definições existentes. Os recursos visuais 0.3.0 continuam os mesmos: não reinstale se já estiverem carregando. F10 > Visual assets: diagnose verifica os dicionários. Instruções de OIV/DLC: VISUAIS-0.3.0.md.

Não execute a PSRP.dll de referência junto do RPCore, pois ambos podem tratar as mesmas teclas e desenhar interfaces simultaneamente.

## O que mudou

### Dentro do carro

O velocímetro deixa o painel retangular no canto direito e passa para o centro inferior: velocidade, marcha e unidade, barra de RPM, combustível e seus ícones, cinto, cruise, faróis e pneu. Fonte e medidas seguem a UiSystem da DLL de referência. RPM/velocidade/marcha/pneus/luzes vêm do veículo; combustível, distância e cinto/cruise continuam ligados aos sistemas existentes do RPCore. Não altera handling, consumo salvo, capacidade do tanque ou cadastro de veículos.

A unidade padrão do desenho novo é mph, como no print. F11 > Vehicle Settings permite km/h. A data/hora são do GTA. Bairro e rua aparecem junto do minimapa quando VehicleHud/Location está ativo. A placa de limite aparece somente nas ruas cadastradas em Visuals/SpeedLimits.json (Davis Ave, Grove St, Forum Dr, Strawberry Ave inicialmente, valor 40), sem inventar limites em ruas desconhecidas. São regras de exibição configuráveis, sem aplicação de multas ou integração policial. O ícone de óleo continua sendo alerta de saúde do motor; não foi adicionado um sistema de troca de óleo.

### Canto superior direito

Seis círculos com as cores originais: saúde, colete, água, comida, stamina e sono. Grupo de 275 pixels virtuais, margem direita 10, topo 20 em canvas de altura 1080; o grupo anterior tinha um passo de 7 pixels sobrando. MoneyUI/BankUI usam as margens transparentes reais e os deslocamentos da referência. Dinheiro abaixo de 1000 mostra duas casas (39.00); valores maiores ficam compactos. O símbolo de dólar já existe na imagem, sem duplicação no texto.

A zona segura nativa não desloca mais esse grupo por padrão. Interface.ini > Layout > RespectSafeZone=True permite habilitá-la; há opção no F11 > UI Settings. NeedsTop padrão passa a 20. Valores 60 e 12, padrões anteriores, são interpretados como 20 sem regravar o INI; demais valores personalizados permanecem válidos.

### Inventário I

Substitui a lista azul padrão por grade 5×5, conforme a imagem inventory.png. A revisão da PSRP.dll enviada contém outra grade 6×4 com crafting; prevaleceu o print pedido pelo usuário. O fundo arredondado e as células são as imagens Background/InventoryBox originais do OIV. Foram medidas as regiões com alpha: Background=(107,126)-(960,884), InventoryBox=(11,11)-(117,117). A transformação considera essas margens transparentes, em vez de tratar a textura toda como área visível.

Quantidade, peso por pilha, nome, ícone, peso total, fome/sede/dinheiro e painel lateral vêm dos dados reais. A durabilidade mostra n/a, porque esse sistema ainda não existe no RPCore. IDs sem definição ou imagem continuam no save e aparecem como desconhecidos, sem consumo automático.

- Setas: selecionar célula. PgUp/PgDn: páginas adicionais quando há mais de 25 tipos.
- Enter: usar o item pelas regras existentes; não cria itens ou altera seus preços/pesos para copiar os valores do print.
- C: selecionar origem; setas e C/Enter: trocar com destino, inclusive vazio. É somente ordenação, sem transferência de quantidades.
- Delete solto: descartar uma unidade. Segurar por 0.9 s: descartar a pilha inteira. Fechar/pausar/morrer cancela um descarte ainda pendente. O descarte é intencional e salva a remoção.
- I/Backspace/Esc: fechar. Controle: direcional, aceitar, X para mover, cancelar para fechar; descarte é apenas por teclado nesta entrega.

O layout é o campo suplementar GridSlots no mesmo Inventory.json. Items, Containers e campos desconhecidos são mantidos. Trocar de perfil não mistura layouts. A grade não limita o inventário a 25 IDs; cria páginas. O armazenamento, lojas e efeitos de uso continuam nos mesmos sistemas. A opção F11 > UI Settings > Inventario em grade pode retornar à lista anterior. O modal consome suas teclas e não abre rádio, cruise, outros menus ou o radial ao navegar. Main publica o estado do modal antes dos ticks: lojas não processam E por polling sob a grade/radial/celular. Cruise cancela ao abrir um modal, como já acontecia com os menus anteriores.

### Celular F1

Usa PhoneTexture, PhoneWallpaper3 (o carro vermelho), os onze ícones do print, relógio grande em AM/PM, dia da semana e quatro linhas de aplicativos. Sem rótulos de atalhos ou caixa azul sobre o wallpaper da tela inicial. Coordenadas do quadro, relógio/dia e ícones seguem as medidas da DLL; o frame completo ocupa a mesma área original (1662,621)-(1904,1062) em 1920×1080. Em ultrawide, ancora à direita mantendo proporções.

Funções disponíveis:

- Contacts: atalhos para banco, garagens, pizza e HQ; não faz ligação de voz.
- Messages / Notifications: histórico local real dos itens usados. Badge somente quando há entrada não lida; nada é simulado como SMS recebido. Abrir marca como lido.
- Notes: criar/editar com Enter, excluir com Delete; até 30 notas, salvas por perfil.
- Fitness: saúde/colete/necessidades reais.
- Clock: hora do GTA e cronômetro iniciado/resetado com Enter.
- Radio: abre o rádio existente.
- Calculator: expressões de dois operandos com +, -, * ou /; não executa código. Divide por zero/overflow são rejeitados.
- Search: procurar atalhos reais do RPCore.
- Settings: alternar os dez wallpapers originais e acessar todos os atalhos anteriores (carteira, inventário, garagens, bolsa, rádio, gangues, pizza, configurações).
- Uber: ícone preservado para a composição solicitada; a página informa que o serviço de passageiros ainda não foi implementado. Não simula um serviço de transporte funcional.

Phone.json mantém notas, wallpaper e histórico, preservando campos desconhecidos. F1 fecha; Backspace/Esc volta à tela inicial e depois fecha. Setas/Enter e controle são suportados. O prop continua cosmético; uma falha ao gravar histórico não pode abortar consumo do inventário.

### J e F11

O radial mantém os oito setores originais. Os ícones agora são desenhados em texturas de 100×100, como na referência: antes eram 70×70, mas PhoneIconRad só possui pixels visíveis em (35,33)-(93,95), tornando o desenho pequeno. Texto central e instruções usam a renderização/fonte da LemonUI em coordenadas 1080, com medidas da referência.

O F11 mantém as oito categorias e PSRPBANNER2, agora com offset (40,8), largura 420, fonte condensada do cabeçalho e opção de zona segura. Continua funcional conforme INTERFACE-0.4.0.md; não foram adicionados sistemas avançados de gangues, aparência, chamadas/SMS ou mapa de objetos. F2 permanece para garagens e F10 para banco/perfis/diagnóstico.

## Testar no GTA

1. Confirmar 0.5.0 no log e texturas carregadas pelo diagnóstico. Capturar a mesma resolução/câmera para comparar com as imagens de referência; elas são recortes/versões diferentes, portanto igualdade visual final não foi demonstrada aqui.
2. Dentro de um carro, conferir painel no centro inferior, marcha, mph/kmh, RPM, cinto, luzes, setas e combustível sem sobreposição. Abastecer e verificar a instrução acima do novo painel. Confirmar ruas/bairro e placa em Davis Ave; ruas sem regra não devem inventar placa.
3. Conferir os seis círculos no alto à direita e dinheiro/banco logo abaixo, inclusive valores baixos e acima de 1000. Testar escala e zona segura em 16:9 e ultrawide.
4. Abrir I, navegar, usar um item, mover para uma célula vazia, fechar e reiniciar. Quantidades, baús e porta-malas devem permanecer corretos. Testar mais de 25 tipos, item desconhecido, troca de perfil e cancelamento de descarte ao fechar/pausar. Descartar somente itens de teste.
5. Abrir F1, comparar wallpaper/relógio/ícones, testar notas, calculadora, pesquisa, histórico e todos os atalhos. Confirmar que os avisos de chamadas/SMS/Uber pendentes são explícitos. Trocar perfil e voltar para verificar isolamento/persistência.
6. Abrir J e conferir tamanho dos ícones, setores, Q/E/Enter e texto. Abrir F11 e conferir banner, posições, submenus e retorno. Não abrir múltiplos modais ao mesmo tempo.
7. Repetir pizza, compras, inventário, rádio, bancos, garagens e veículos já existentes. Se houver falha, enviar RPCore.log/ScriptHookVDotNet.log e uma captura do resultado atual.
