# Interface RPCore 0.4.0

Implementação própria baseada nos prints, no catálogo dos YTDs enviados e em inspeção somente de leitura da PSRP.dll enviada. A DLL PSRP não é uma dependência e não foi incluída no pacote. A arte original permanece atribuída ao JMC MODS, sem alteração dos OIV/RPF.

## Instalar ou atualizar

Feche o GTA, extraia RPCore-update-0.4.0.zip fora do jogo e execute o Install-RPCore.ps1 com a pasta do GTA. O instalador guarda backup e preserva todos os arquivos já existentes em scripts/RPCore, incluindo saves e configurações. Copia Interface.ini se ausente. Se os recursos PSRP já funcionam, não precisa reinstalar o OIV/DLC. Caso contrário, siga VISUAIS-0.3.0.md com o pacote visual 0.3.0, que continua sendo o mesmo.

Não instale a PSRP.dll de referência junto do RPCore: os dois scripts podem desenhar HUDs e tratar teclas simultaneamente. Esta versão foi feita para funcionar de forma independente.

## J — radial

Oito setores originais RadMenu1 a RadMenu8 e RadMiddle de PSRP_Inventory. PSRP_RADICAL tem nomes parecidos mas não possui RadMenu8; não é o conjunto usado nesta tela. Ícones originais, setor vermelho selecionado, texto central, orientação Q/E/Enter. J/Esc fecha. Controle: direcional esquerdo/direito, aceitar/cancelar quando aberto.

| Posição, a partir de cima e em sentido horário | Ação real |
|---|---|
| Phone | Abre o telefone F1 existente, a pé |
| Inventory | Abre o inventário existente |
| Emotes | Acenar, aplaudir, parar somente o gesto RPCore |
| Vehicle Control | Menu existente, exige motorista de veículo compatível |
| Fast GPS | Garagens, PDM, gangues e pontos pessoais |
| Gangs | Menu de afiliação/HQ existente |
| Interactions | Inventário, armazenamento, pizza e descanso |
| Settings | Abre o F11 |

Radial/modal consome suas teclas: E não abre uma loja e Q não procura cobertura durante a seleção. Não modifica velocidade do tempo ou congelamento do personagem. Fecha em morte, pausa, cutscene ou missão. Falta de texturas permite uma lista básica funcional. Sistemas desativados continuam respeitados.

## F11 — Settings Menu

O banner do carro preto do segundo print é PSRPBANNER2. PSRPBANNER1 mostra um carro branco. As oito categorias usam o banner e os submenus LemonUI, com retorno pelo comando normal de voltar.

| Categoria | O que foi implementado nesta entrega |
|---|---|
| Gang Creation | Nomear gangue, definir HQ na posição atual, criar blip, usar GPS/afiliação. Salva em Gangs/RPCoreCreated.json sem alterar CustomGangs.json original. Até 50 criações. |
| Ped Settings | Ver status, abrir perfis/importação/banco, salvar ponto pessoal. Não troca modelo ou roupa. |
| Survival Settings | Ajustar consumo de fome/sede, persistir em Settings.ini e descansar. |
| Vehicle Settings | Controles, garagem, concessionária, consumo L/100km persistente. |
| UI Settings | Ligar/desligar HUDs de necessidades/veículo/dinheiro, escala, ocultar minimapa a pé. |
| Game Settings | Salvar/configurar/diagnosticar, rádio, conquistas. |
| Cheat Menu | Cura, colete 100, adicionar $1000 por ação explícita, fora de missão. |
| Map Builder | Editor inicial de pontos nomeados: salvar na posição atual, blip, GPS e excluir. Até 100 por perfil. Ainda não posiciona objetos/prédios. |

Garagens passam para F2 e continuam no F11 > Vehicle Settings e no radial GPS. Uma configuração antiga Garage=F11 é remapeada para F2 somente em memória, preservando o INI. F10 conserva banco/perfis/importações/diagnóstico. Interface.ini controla J e F11; evite atribuir a mesma tecla a outras ações.

## HUD

Seis círculos pequenos no alto à direita: saúde, colete, água, comida, stamina e sono. Usa CircleWhite/OutBar0..100 e os ícones originais. Saúde/colete/stamina vêm do GTA; fome/sede/sono vêm do perfil. Dinheiro/banco usam as imagens MoneyUI/BankUI e valores compactos, como 500.3K. Inventário pessoal recebe banner azul com o título manuscrito Inventory; lojas mantêm sua arte de loja.

Sono é um novo campo em Needs.json, com padrão 100 para saves antigos. Cai 0.25/minuto, sem dano adicional. Descansar parado a pé recupera 60/minuto; mover-se, combate, veículo, missão ou morte encerram o descanso. Não passa o relógio nem altera tarefas do ped. Pausa/cutscene não consomem necessidades. Importações novas também leem Sleep do PSRP; perfis já importados não são reimportados ou sobrescritos.

Visuals.ini mantém escala e NeedsTop. O valor 60, padrão antigo, é interpretado como 12 no layout novo sem regravar o arquivo; outros valores são respeitados. O sono e MapPins.json ficam no perfil; gangues criadas são configurações compartilhadas. Campos desconhecidos dos saves continuam preservados.

## Testar dentro do GTA

1. Confirmar RPCore 0.4.0 no log; usar F10 > Visual assets: diagnose caso falte arte. J deve mostrar oito setores, vermelho no selecionado e ícones brancos. Conferir Q/E, Enter e J/Esc; não abrir loja ao navegar com E.
2. Testar as oito ações, incluindo restrições a pé/no veículo e sistemas desativados. F1 e F10 continuam acessíveis. Testar controle e resoluções 16:9/ultrawide, safe zone e escala.
3. F11 deve mostrar o carro preto e as oito categorias do print. Entrar/voltar em cada submenu; F2 deve abrir somente garagem.
4. Conferir seis indicadores e valores reais; usar colete, correr, consumir comida/água. Descansar, observar sono subir, mover-se para interromper; reiniciar script e verificar persistência.
5. Mudar fome/sede/HUD/escala, reiniciar e conferir. Desativar ocultação de minimapa se outro mod também controla o radar.
6. Criar gangue de teste, verificar HQ/F4 e persistência; verificar que as três gangues originais continuam disponíveis. Esse cadastro não cria NPCs, territórios ou guerras.
7. Criar ponto de teste, usar GPS, reiniciar, trocar perfil e excluir apenas o ponto criado. Não deve alterar pontos de outros perfis ou blips de outros mods.
8. Testar acenar/aplaudir e parar; morrer/entrar em carro ou missão durante o gesto. Não deve limpar tarefas de outros scripts.
9. Conferir inventário, carteira, veículos, garagem, combustível e demais funcionalidades já testadas na 0.3.0. Manter backup antes do primeiro teste.

## Limites

Os prints orientam a composição; igualdade visual final exige teste de renderização dentro do GTA. Este ambiente não executa GTA/Windows/OpenIV. Não foi transplantada toda a lógica da PSRP.dll. Não há telefonia/chamadas/SMS, third-eye com NPCs, editor de aparência, mapa de objetos ou guerras avançadas nesta entrega. Não há opções vazias que prometam essas funções: as ações acima descrevem exatamente o que está disponível.
