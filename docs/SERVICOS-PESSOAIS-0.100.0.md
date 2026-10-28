# Roupas, barbearia, acabamento e moradia — 0.100.0

Este grupo da fase8 compara `MultiplayerCharacter.BuildClothingMenu/AddClothingComponent/AddPropItem/SaveOutfitForCharacter/ApplyOutfit/BuildBarberMenu/Update`, as páginas de roupas/GPS do `RadialMenu` e a entrada/saída/guardarroupa de `Apartments` da PSRP.dll descompilada. A DLL de referência não foi executada ou instalada junto do RPCore.

## Como acessar

- **Roupas:** vá ao ícone RPCore de roupas, a pé, e pressione **E no marcador**. São os14 pontos de `BuyClothesMarkers` da referência: Textile City, Vespucci, Del Perro, Harmony, Hawick, Chumash, Rockford Hills, Burton, Morningwood, Strawberry, Grapeseed, Route68, Grand Senora e Paleto Bay. O guardarroupa do Eclipse continua disponível para o proprietário. **J > GPS > Loja de roupas mais próxima** e **F11 > GPS > Mais próximos > Loja de roupas** indicam o ponto. O GPS de garagens permanece no F11; o setor do radial antes usado para garagem passa a roupas, conforme a referência.
- **Barbearia:** **E no marcador RPCore** ou **F10 > serviços pessoais > Barbearias**. Corte/textura e olhos mantêm a prévia existente, com confirmação e taxa configurada. Acabamento abre o serviço separado de tintura/barba/rosto já existente.
- **Moradia:** entrada física do Eclipse ou menu de moradia do F10. Compra abre confirmação com **Cancelar selecionado primeiro**. Preço, propriedade, interior, armazenamento e saída pública continuam usando os dados existentes.

## O que mudou

`WardrobeSystem` usa as lojas, além do apartamento, com ícones próprios e destinos reais. A primeira abertura cria `scripts/RPCore/Services/Clothing.json` se ausente; localizações personalizadas válidas são usadas, e arquivo inválido permanece intacto enquanto se usam os defaults internos. A biblioteca anterior é a mesma: não há importação duplicada de conjuntos, mudança de modelo ou cobrança nova por roupa. Como na edição de roupas examinada na referência, este fluxo não inventa preços por peça.

Editor mantém cabeça/cabelo/modelo e oferece os10 slots de roupa e5 de acessórios já suportados. Slots sem variações instaladas são omitidos; acessórios têm opção sem acessório. Troca de peça reinicia textura, com validação nativa anterior à aplicação. Salvar novo, vestir, renomear, substituir e excluir continuam por perfil/modelo. Substituição e exclusão agora abrem confirmação com Cancelar primeiro; renomear/substituir/excluir conferem o registro completo que foi apresentado, inclusive metadados, para não operar sobre uma versão que mudou. Nenhum conjunto antigo é truncado quando se reduz a capacidade configurada.

Acabamento passa a categorias: tintura/mechas, barba, sobrancelhas, pele/maquiagem, herança e traços. O atendimento apresenta resumo dos grupos antes da confirmação. As opções de selecionar grupos são escolhas do rascunho para preservar o restante do rosto, não novos interruptores globais do mod. Continuam a aplicação paga retomável, reuso gratuito do visual registrado e o estorno permitido pelo estado anterior do pedido. Encerrar pendência sem estorno também começa em Cancelar. A sentinela nativa255 para overlay ausente permanece normalizada e testada.

Os quatro serviços compartilham despacho de ações **depois de ObjectPool.Process**, evitando limpar/reconstruir a lista enquanto LemonUI processa sua ativação. Cada ação confere página, ator, modelo, perfil, visibilidade, contexto e tempo máximo de1s. Botão mantido não confirma repetidamente. Seleção é lembrada por página; Back/Escape/B volta ao pai nas páginas internas. Fechar a raiz encerra o menu. Não há novas teclas globais ou flags de ativação.

Prévia de roupa/cabelo cancela ao fechar, afastar, perder controle ou entrar em contexto incompatível. O cancelamento só restaura o snapshot se o visual atual ainda corresponde à última prévia RPCore; se outro sistema alterou a roupa/cabelo, conserva essa alteração em vez de sobrescrevê-la. Nesse caso também não continua editando/salvando a prévia antiga. Orientação da barbearia só retorna à anterior se ainda estiver na orientação deixada pelo RPCore. Não se restaura um snapshot de outro perfil/modelo. `CharacterSystem.SaveBody` não captura durante os leases de roupa/barbearia/acabamento, evitando gravar prévia não confirmada no Character.json.

Moradia revalida propriedade/dados durante o carregamento de entrada, além das provas de ator/perfil/piso/ocupação existentes. A saída pública continua permitida a não proprietários, inclusive mantendo a regra anterior de não exigir zerar procurado para sair. A verificação física do interior não depende da flag de inventário aberto: o armazenamento privado continua utilizável enquanto sua própria interface está aberta.

## Preservação e limites

Não mudam preços anteriores de barbearia/acabamento/apartamento, peças já salvas, contas, inventário, configurações existentes, teclasF2/J/F11, RPF/OIV/YTD ou áudio. A criação/carregamento de personagens não foi reimplementada neste grupo; apenas o bloqueio de snapshot durante prévias foi acrescentado. DMV, seguro, demais serviços e a fase9 continuam no roteiro de refinamento.

Usa a câmera normal do GTA e os banners já configurados no RPCore. Não acrescenta a câmera dedicada da PSRP, nem menus de tatuagens, catálogo precificado de roupas ou restauração universal da aparência de outros mods. Os looks de acabamento continuam sendo valores novos selecionados, sem prévia nativa durante o rascunho; herança paga parcialmente aplicada continua retomável, sem promessa de recuperar um rosto anterior não capturado. Não há garantia de crash-atomicidade entre mudanças nativas e gravação em arquivo.

## Roteiro dentro do GTA

1. Atualize pelo instalador da pasta extraída e recarregue os plugins. Confira F2/J/F11 e seu saldo/conjuntos anteriores.
2. J > GPS > roupas, vá ao ponto e use E. Confira ausência de abertura simultânea do menu nativo. Teste pelo menos uma loja de cada região e um personagem freemode de cada sexo; teste protagonista, DLC de roupa instalado e slots sem variação.
3. Edite roupa/acessório/textura; volte de peça para categorias por Back/Escape/B. Feche a edição ou saia do marcador: visual anterior retorna. Entre em outra loja: acesso não deve ficar preso à loja anterior.
4. Salve novo conjunto, vista, renomeie, substitua e exclua. Nas duas confirmações, Enter inicialmente deve apenas cancelar; confirme manualmente na segunda linha. Confira outras roupas e saldo intactos, inclusive após reload. Ped incompatível não deve ser convertido.
5. Mantenha Enter/botão do controle ou mouse pressionado ao navegar/confirmar. Não deve executar várias ações. Troque de perfil, pause ou abra o celular durante uma prévia: a ação pendente não deve atingir outro personagem.
6. Em um personagem RPCore, aguarde um autosave durante uma prévia, cancele e recarregue; roupa/cabelo provisórios não devem entrar no Character.json. Com outro mod mudando a roupa, cancelar não deve sobrescrever a mudança externa.
7. Na barbearia, E abre RPCore; corte/textura/olhos, girar, cancelar, confirmar com e sem saldo, reaplicar confirmado sem nova taxa. Confira o banco e a taxa existente uma única vez.
8. Acabamento: percorra todas as categorias, selecione apenas alguns grupos, confira resumo, volte e cancele sem efeito/cobrança. Confirme; reaplique registrado grátis; teste ausência de barba/maquiagem. Em herança, interrompa e retome o pedido pago: sem segunda cobrança.
9. Eclipse: cancele compra, confirme uma vez, entre/saia; imóvel já adquirido não deve cobrar de novo. Interior sem piso/ocupado deve recusar movimento. Guarde/retire itens com a interface privada aberta e confira guardarroupa do andar superior.

Compilação, testes de dados e instalação simulada não substituem esses testes nativos. Consulte VALIDACAO-0.100.0.md.
