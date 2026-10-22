# RPCore 0.3.0 — recursos visuais PSRP

Esta versão integra os recursos enviados em **PSRP.oiv** e **psrp_props.zip**. O autor declarado dentro do OIV é **JMC MODS**; os arquivos e a atribuição original foram preservados. A composição do HUD, os vínculos com o RPCore e a navegação foram implementados em C# neste projeto. Os arquivos de texturas não contêm o código/layout completo do PSRP; esta entrega não afirma reproduzir todo o comportamento do mod original.

## O que foi analisado

- OIV: arquivo ZIP com `assembly.xml`, ícone e oito YTDs, destinados a `update/update.rpf/x64/data/cdimages/scaleform_generic.rpf`. São **679 texturas**: 250 em PSRP, 298 em PSRP_Inventory, 31 banners, 9 em PSRP_Jobs, 80 no telefone, 9 em PSRP_RADICAL e dois contatos individuais.
- Props: `dlc.rpf` de **127.979.008 bytes**, RPF7 OPEN, com `content.xml`, `setup2.xml`, `props.rpf`, um YTYP e 27 modelos YDR/YFT. Os 28 recursos RSC7 foram descomprimidos para inspeção e seus tamanhos de páginas conferidos; os originais não foram regravados.
- Os hashes de 22 nomes de modelos aparecem no YTYP. Os quatro modelos de caixas YFT e `seabream` não tiveram seus hashes de nome localizados nessa verificação; isso não prova isoladamente que sejam utilizáveis ou inválidos no jogo. Não foram vinculados a novas ações. O diagnóstico no GTA informa a disponibilidade real dos 27 nomes.
- Catálogo de nomes/dimensões: `config/Visuals/Catalog.json`. Manifesto/estrutura/XML originais: `docs/visual-inspection.json`. Pré-visualizações extraídas, sem alterar os YTDs, ficam em `docs/visual-previews`.

## Baixar e instalar

São **dois pacotes separados**: `RPCore-update-0.3.0.zip` (DLL/configurações) e `RPCore-visual-assets-0.3.0.zip` (OIV e props originais). Os fontes ficam em `RPCore-source-0.3.0.zip`. O ZIP de fontes não duplica os binários visuais; eles estão no pacote visual e em `visual-assets` no repositório.

Se os mesmos recursos PSRP já estão instalados no seu GTA, primeiro atualize a DLL e use o diagnóstico; pode não ser necessário reinstalar o OIV/DLC.

1. **Feche o GTA.** Atualize o RPCore pelo `Install-RPCore.ps1` do pacote de atualização, como nas versões anteriores. Ele faz backup e preserva saves/configurações existentes. Os novos `Visuals.ini`, `Visuals/Catalog.json`, `Visuals/Bindings.json` e item pack são copiados se ausentes.
2. Extraia o pacote visual em uma pasta temporária fora do GTA. Ele contém PSRP.oiv, psrp_props.zip e Prepare-VisualAssets.ps1.
3. Para preparar o DLC de props, abra PowerShell nessa pasta e execute:

   ```powershell
   .\Prepare-VisualAssets.ps1 -GtaPath 'D:\Games\Grand Theft Auto V'
   ```

   O helper verifica SHA-256 dos dois arquivos enviados e do RPF extraído. Copia somente para `mods/update/x64/dlcpacks/psrp_props/dlc.rpf`. Se já houver o mesmo RPF, conserva e informa. Se o existente for diferente, interrompe sem sobrescrever: compare a versão e faça backup antes de decidir substituí-la manualmente.
4. No **OpenIV → Tools → Package Installer**, selecione PSRP.oiv e escolha instalar na pasta **mods**. O pacote original adiciona seus YTDs ao `scaleform_generic.rpf` da cópia `mods/update/update.rpf`. Preserve um backup da sua cópia mods antes dessa etapa, especialmente se já usa recursos com os mesmos nomes. Não substitua seu update.rpf inteiro por outro arquivo.
5. Ainda no OpenIV, abra **mods/update/update.rpf/common/data/dlclist.xml**. Dentro de `<Paths>`, acrescente apenas se ainda não existir:

   ```xml
   <Item>dlcpacks:/psrp_props/</Item>
   ```

   Mantenha todas as entradas dos seus outros DLCs/mods. O helper não edita update.rpf, dlclist.xml nem os arquivos originais do jogo.
6. Inicie GTA V Legacy no modo história, com Enhanced 1.1.0.6 e LemonUI 2.2. F10 deve identificar **0.3.0**. Escolha **Visual assets: diagnose**, aguarde seis segundos e confira RPCore.log. Dicionários carregados e modelos registrados são evidências de disponibilidade; o desenho/animação ainda precisa ser observado no jogo.

## O que muda no jogo

- **Necessidades:** ícones HealthIcon/FoodIcon/DrinkIcon e anéis OutBar0–100, ligados à vida, fome e sede reais. Não foi exibido SleepIcon com progresso inventado: sono continua sem sistema próprio.
- **Carteira:** dinheiro/banco reais do perfil, com MoneyIcon.
- **Veículo:** moldura RectangleBorder, SpeedoLine, velocidade, marcha, RPM, combustível/distância quando controlados pelo RPCore; indicadores de cinto/cruise/luzes, alerta de motor e pneus. Os dados vêm dos sistemas existentes/natives. O ícone CarOilIcon indica alerta pela saúde do motor; não representa uma simulação nova de óleo.
- **Banners LemonUI:** imagens originais nos menus de inventário, lojas, armazenamento, veículo, garagem, concessionária, bolsa, configurações e outros menus existentes. Nenhuma alteração de save é necessária.
- **Pizza:** banner PizzaBanner original ligado ao contador/prazo real das entregas, com fallback do texto anterior.
- **Itens:** ícones originais nas linhas de inventário, loja e armazenamento. O item pack PSRPConsumables.json acrescenta leite, batata frita, hotdog, Pepsi, Sprite, Monster, Red Bull, muffin, kiwi e pera, com efeitos de comida/bebida e preços próprios do RPCore.
- **Props ao consumir:** quando a pé, vivo, desarmado e fora de combate/missão, o consumo apresenta um prop na mão com animação curta. Usa modelos do DLC quando disponíveis e fallback nativo em parte dos itens. Se o modelo não existir/carregar, o consumo funcional continua; não há item/recompensa falso para depender do visual. Terminar, morrer, entrar em veículo, trocar perfil ou abortar remove apenas o prop/animação iniciados por este sistema.
- **Telefone:** **F1** a pé abre a moldura/wallpaper/ícones PSRP. Setas + Enter selecionam; Esc/F1 fecham. Controle: direcional/A/B após abrir. Há atalhos para carteira, inventário, garagens, bolsa, rádio, gangues, iniciar/cancelar pizza e configurações. Carteira/configurações abrem o menu F10 existente; não são um aplicativo bancário separado. O relógio é do jogo. Não foram apresentados contatos, mensagens, câmera, Twitch, polícia ou clima como aplicativos funcionais.
- **Fallback:** sem os dicionários, conserva HUD básico e menus funcionais; telefone usa desenho simples. Após dez segundos sem PSRP carregado, informa uma vez. Se o recurso visual falhar, o sistema visual é isolado e os dados continuam nos sistemas anteriores.

## Configurar

`scripts/RPCore/Visuals.ini` tem opções independentes para HUD de necessidades/veículo/carteira, banners, ícones, props e telefone. `Enabled=False` retorna ao visual básico. `Scale` aceita 0,6–1,4 e `NeedsTop` controla a altura dos indicadores. As posições respeitam proporção da tela e safe zone do GTA; teste ultrawide e sua configuração pessoal. `Keys/Phone` muda F1 em caso de conflito com trainer.

`Visuals/Bindings.json` associa IDs a ícones, modelos e banners. Offsets X/Y/Z e rotações RX/RY/RZ de props podem ser ajustados nos vínculos; `_phone` é o vínculo especial do telefone. Os nomes/dimensões foram lidos dos YTDs reais, não inferidos por aparência. Para reler opções/vínculos, use F10 → Reload configuration; reinicie o script se precisar limpar recursos já carregados. Não edite os saves para alterar o visual.

## Testar esta versão

1. Confira se inventário, pizza, cinto, combustível e garagem da base continuam funcionando com seus saves.
2. F10 → diagnose: aguarde seis segundos; registre contagem de dicionários/modelos e eventuais ausências no log.
3. Fique a pé e observe anéis de vida/fome/sede; consuma água/comida e confira mudança. Pause, veja cutscene e esconda HUD: não deve haver indicadores sobre essas telas.
4. Abra loja, inventário e armazenamento: compare banners e ícones; compre/use um item e confira dinheiro, peso e quantidade. Sem textura, texto/operações devem permanecer.
5. Dirija: confira velocidade, marcha/RPM e os estados reais de cinto, cruise, setas e luzes. Teste pneu furado e veículo danificado. Abasteça: mensagem de parada não deve cobrir o painel.
6. A pé, desarmado e fora de missão, use leite/Pepsi/hotdog/fries; confira prop/animação e desaparecimento após poucos segundos. Teste morrer/entrar em veículo/trocar perfil durante a apresentação e confirme limpeza. Armado/em combate não deve trocar sua animação por consumo.
7. F1: confira moldura, wallpaper, seleção e cada atalho. O frame tem tela preta opaca no arquivo original e é desenhado antes dos aplicativos. Teste Esc/F1, morte, pausa, missão e entrada em veículo: deve fechar/limpar o prop. Nunca deve disparar ações de outros atalhos enquanto navega nas setas.
8. Teste 1920×1080 e sua resolução/safe zone. Observe se sobrepõe estrelas, minimapa ou HUD de outros mods. Ajuste Scale/NeedsTop e informe o ponto de sobreposição.
9. Na sua cópia de teste, desative Enabled ou remova temporariamente o pacote visual: a DLL deve continuar com HUD básico/funcionalidade, sem apagar progresso. Não retire arquivos da instalação principal para testar corrupção.

Em falha, informe etapa/resolução e envie RPCore.log/log SHVDN. Compilação e testes automatizados estão em VALIDACAO.md; o GTA/Windows e a instalação pelo OpenIV serão testados por você. A versão 0.2.0 permanece no commit `ccc5637fb9b1deb62824ef2dd8c3d825dc0b2115` para retorno.
