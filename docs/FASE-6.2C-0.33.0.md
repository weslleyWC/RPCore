# RPCore 0.33.0 — fase 6.2C: armazenamento privado do apartamento

## Instalação

Extraia RPCore-update-0.33.0.zip. Abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Ajuste o caminho. GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não execute PSRP.dll como segundo script; é referência de análise. Sem reinstalar .oiv/.rpf. Preserva propriedade, roupas, itens, configs e demais sistemas; acrescenta Jobs/HomeStorage.json se ausente. F2/F11 e F8 anteriores mantidos.

## Como usar

Entre no Eclipse adquirido. Dentro dele: F10 → Moradia / Eclipse Towers → Armazenamento do apartamento. O menu funciona de qualquer ponto dentro do seu apartamento, sem depender do marcador. Também há E no ponto de conveniência próximo à entrada interna: (-791.8, 315.8, 187.9136). Este ponto é próprio do RPCore e configurável; se seus recursos alterarem o local, use o menu dentro do imóvel.

Escolha Guardar ou Retirar e depois a quantidade: uma unidade, até cinco ou a pilha inteira. Alternativas iguais aparecem só uma vez. Capacidade padrão do depósito: 200kg; retirada respeita a capacidade atual da mochila. Se a quantidade escolhida não couber, recusa tudo, sem transferência parcial. O cabeçalho mostra peso guardado/capacidade. Fechar armazenamento ou Esc encerra o menu e libera a atividade.

Usa os mesmos itens do inventário I e Inventory.json, em Containers[home:eclipse], por perfil. Não cria outra bolsa e não muda dinheiro, roupa ou propriedade. Outros stashes e porta-malas mantêm suas chaves e conteúdos. Itens com definições temporariamente ausentes permanecem no save e usam o peso alternativo já existente (0,1kg). Bolsas especializadas de caça/pesca/crafting e cargas de trabalho continuam nos seus sistemas; não são convertidas automaticamente em itens do inventário I.

Acesso exige perfil proprietário dentro do apartamento, a pé, fora de procurado/combate/missão. Sair, morrer, trocar perfil, pausar ou abrir uma interface modal fecha o acesso. Perfil não proprietário que esteja dentro ainda pode sair, mas não abrir o depósito. O sistema não deixa retirar itens à distância pelo F10.

## Correção integrada

O fluxo anterior do InventorySystem.Transfer movia itens em memória antes de salvar e não restaurava as quantidades se Save falhasse. Agora monta uma cópia completa, atualiza os dois lados e o layout, grava Inventory.json e só então publica o novo estado. Em erro de gravação, conserva o estado anterior e informa a falha. Essa correção também atende aos depósitos/porta-malas antigos, mantendo a operação de uma unidade no F8. Abrir armazenamento vazio não cria nem salva um container por si só.

## Roteiro de teste no GTA

1. Atualize e confirme propriedade Eclipse, biblioteca de roupas, banco/caixa, mochila e conteúdos antigos. Entre no apartamento e abra o armazenamento pelo F10. Fora do apartamento ou em perfil sem propriedade deve recusar acesso.
2. Com dez garrafas de água na mochila, guarde uma, depois cinco e depois o restante. Confira diminuição na mochila e aumento igual no depósito. Retire uma, cinco/pilha e confira conservação das quantidades, peso e layout do inventário I.
3. Guarde também outro item e reinicie scripts/jogo. Ambos devem continuar guardados; retirar não pode duplicar nem apagar itens. Outros depósitos/porta-malas devem conservar seus conteúdos. Confira que não cobra dinheiro nem muda roupas.
4. Para testar falta de espaço com pouco estoque, opcionalmente altere Capacity para 1 em Jobs/HomeStorage.json com o jogo fechado. Cinco águas pesam 2,5kg: a pilha deve ser recusada inteira; duas águas cabem exatamente. Retirada precisa de espaço na mochila. Restaure 200 depois. Reduzir capacidade não apaga itens já guardados; permite retirar, mas bloqueia depósitos que não caibam.
5. Abra depósito, use F2 ou F11, feche e confira atividade liberada. Saia do imóvel, morra ou troque perfil: não deve permitir transferência pelo menu antigo. Perfil novo tem seu próprio Inventory.json e não recebe os itens do anterior.
6. Teste E junto ao ponto configurado. Se a posição estiver ocupada/fora da geometria dos seus recursos, use F10 dentro do apartamento e ajuste somente Point na configuração, sem mudar ContainerId. Não há objeto de baú criado/apagado.
7. Teste o F8 no stash antigo e no porta-malas de carro pessoal destrancado, guardar/retirar uma unidade e recarregar. Veículo de trabalho/ocupações e regras anteriores continuam. No apartamento, repita guardarroupa, salvar/vestir/cancelar roupas, entrada/saída e F2/F11/teleporte; depois confira trabalhos/PDM.

Falha de gravação foi testada em testes automatizados com exceções de Save; não precisa corromper seus arquivos reais para testar. No GTA, se aparecer falha de gravação, confira o log e as quantidades antes de tentar novamente.

## Configuração e referência

Jobs/HomeStorage.json: PropertyId=eclipse, ContainerId=home:eclipse, Capacity=200 e Point do marcador opcional. Não troque ContainerId para renomear o menu: essa é a chave persistente dos itens; mudar abre outra chave sem migrar/apagar a antiga. Se você personalizou o ID da propriedade em Housing.json, ajuste PropertyId correspondente. Configuração inválida usa defaults em memória e conserva o original.

Analisados NewStorageSystem.StorageManager/OpenStorage/Save/Load e NewInventorySystem.MoveOneToStorage/MoveStackToStorage/CreateStorages. A referência usa stash house em outro interior e arquivos separados de storage; o RPCore reutiliza o inventário único e containers já existentes. Ponto Eclipse/capacidade e acesso pelo F10 são decisões próprias, não coordenadas de storage encontradas na PSRP.

Não implementa móveis, animação de baú, armazenamento compartilhado entre perfis, hotéis/aluguel ou conversão automática de stashes legados. Demais diferenças e fases 8/9 permanecem. Veja VALIDACAO.md e PSRP-SYSTEMS-AUDIT-0.33.0.json. GTA não executado nesta nuvem.
