# RPCore 0.38.0 — fase 6.3E: criar e carregar personagens

## Instalar e abrir

Extraia RPCore-update-0.38.0.zip da branch rpcore-entrega. Abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2 mantidos. Não precisa reinstalar .oiv/.rpf nem executar PSRP.dll junto. Instalador conserva arquivos existentes; os novos arquivos de personagem surgem somente pelas ações no jogo.

Abra **F10 → Personagens / criar e carregar**. Para trocar, fique parado a pé, sem procurado, combate, missão, cutscene, outra câmera ou atividade com reserva em uso. Criar o cadastro e escolher o modelo não troca seu ped; há uma confirmação separada de carregamento.

## Criar e entrar

1. Criar novo personagem: escolha nome (até 40 caracteres) e modelo masculino/feminino freemode. Nome é exibido na lista; o caminho usa um identificador próprio, nunca o texto digitado.
2. Criar perfil e abrir confirmacao: cria uma pasta RP_ seguida de 16 dígitos hexadecimais em SaveData, com Character.json e Wallet.json novos. Não sobrescreve pasta existente nem copia dinheiro, propriedade, itens, trabalhos ou empresa do perfil anterior. Limite de 64 cadastros neste menu.
3. Confirmar troca de personagem: salva o perfil anterior, carrega o modelo no local atual e ativa o novo save. O cadastro já criado permanece na lista se cancelar. Falha antes da troca mantém o ped atual; não há carregamento automático ao instalar/reiniciar.
4. O personagem novo começa sem saldo, armas ou colete, saúde padrão 200, componentes padrão do modelo e rosto base de duas origens com mistura 50%. Necessidades/itens/progresso usam os padrões do perfil novo. Não há compra de imóvel, gangue, emprego ou dinheiro inicial automático.
5. Vá às barbearias pelo F10 para ajustar cortes/olhos, tintura/barba/maquiagem/pele e herança/20 traços das fases anteriores. Roupas e moradia continuam nos sistemas existentes, com o progresso separado do novo perfil.

Carregar um cadastro existente abre a mesma confirmação, restaura seu modelo e usa o perfil correspondente. Os cadastros novos não aparecem no antigo Use profile: esse seletor continua para os perfis antigos/importados, sem trocar ped. Assim a seleção de um personagem cadastrado passa pelo fluxo que carrega seu modelo e aparência.

## O que é salvo e restaurado

Character.json conserva nome/modelo/base facial e snapshot de saúde/colete, os 12 slots de componentes (incluindo cabeça/cabelo), cinco props, armas possuídas, componentes/tintas de armas, munição no pente, arma selecionada e quantidade de cada tipo compartilhado de munição. Usa atribuição por tipo de munição, sem somar novamente o mesmo pool de duas armas. Autosave e Salvar roupas / equipamento atuais atualizam o snapshot do personagem ativo; não gravam por cima de outro modelo/perfil ou de um carregamento pendente.

Antes de substituir um personagem RPCore, captura novamente seu equipamento atual; antes de substituir um protagonista, grava CharacterReturn.json com o modelo, roupa/equipamento e saúde/colete de origem. O dinheiro nativo do protagonista não é alterado pelo carregador. Saldo, inventário RPCore, necessidades, trabalhos e demais progressos continuam nos arquivos próprios de cada perfil, salvos antes de mudar modelo/perfil.

A aparência carregada combina o rosto base do cadastro com os grupos que você confirmou e salvou no Wallet.Grooming, além da cor dos olhos confirmada na barbearia. Sem registro, usa valores padrão explícitos. O head blend termina antes de aplicar traços e acabamento. Os componentes/props salvos conservam as roupas atuais; não importa uma roupa de outro modelo. Nenhum serviço de aparência é cobrado novamente por carregar o personagem.

Não há leitura completa das cores, overlays e morphs alterados por scripts externos. Esses detalhes desconhecidos não são capturados por offsets e podem voltar ao padrão ao carregar o modelo. Não é um clone completo de qualquer ped: animações/tarefas de missão, estados especiais de outros mods, estatísticas e buffs externos não integram o snapshot. A volta ao protagonista restaura os dados de corpo/equipamento descritos, sem prometer captura de qualquer efeito visual externo.

## Voltar e retomar

F10 → Personagens → **Voltar ao protagonista de origem**, depois confirmar. Salva o personagem RPCore e volta ao protagonista registrado antes de sair, com seus dados de corpo/equipamento. Mantém o local atual; não teleporta para o ponto antigo, altera dinheiro do modo história ou inicia missão. O backup registra o protagonista de origem mais recente; não é um seletor livre dos três protagonistas nem substitui os saves nativos do GTA.

Carregamento do modelo e do rosto usa ticks, até dez segundos em cada etapa, sem loop bloqueante, câmera forçada, congelar o jogador ou apagar NPCs/veículos. Atividade fica reservada enquanto carrega; cancelar/afastar/interromper libera a reserva.

Antes da aplicação, o cadastro é marcado ApplyPending. Recurso ausente, falha de gravação, interrupção ou divergência na leitura de roupas/armas/munição deixa o snapshot anterior protegido. Não salva o personagem parcialmente carregado por cima dele. Abra a lista e confirme novamente o cadastro marcado Pendente para tentar restaurar. A volta ao protagonista também tem estado pendente durável e opção Retomar volta ao protagonista, inclusive se o modelo já tiver voltado e faltar equipamento. Uma volta pendente deve ser resolvida antes de substituir seu backup por outra criação a partir de protagonista.

Depois da troca, cancelar pode deixar a aparência parcial no novo perfil; não desfaz automaticamente o modelo ou inventa uma restauração anterior. Retome pela lista. Reiniciar o script apenas lê o cadastro; não executa a troca sozinho. JSON inválido é conservado e protegido contra gravação de padrões; consulte o log/backup para reparar antes de repetir.

## Implementação e referência

Adicionados CharacterData.cs (validação/cadastro atômico/snapshots), CharacterBodyRuntime.cs (captura, recursos e leitura após restauração), CharacterSystem.cs (menus/modelo/espera/retorno). Main agora tem transição explícita que exige salvar os sistemas e a carteira antigos, persistir a seleção e trocar o modelo antes de recarregar os módulos do novo perfil. Falha de save/persistência anterior aborta a troca. Carregamento automático e troca antiga de perfil continuam sem aplicar aparência.

Comparados Gender_ItemChanged, PlayerNameOption_Activated, BuildMenu e LoadCharacterToPed na PSRP.dll. Código próprio. A PSRP troca o ped durante seleção e oferece dinheiro/gangue/local inicial; RPCore separa cadastro/confirmar, mantém o local e inicia saldo zero, com snapshots e pendências próprios. Confira PSRP-SYSTEMS-AUDIT-0.38.0.json, com 75 fontes indexadas e hashes do baseline/DLL.

Criador ainda tem diferenças: câmera/prévia de criação, importação automática da aparência PSRP, terceiro contribuinte, estados externos completos e outras opções não foram declarados concluídos. Fases 8 (interface/mecânicas e ergonomia) e 9 (todos os trabalhos) continuam previstas. Nenhuma mudança no HUD/fome/sede, F2 celular, F11/teleporte, radial J ou recursos .oiv/.rpf nesta entrega.

## Testes no GTA

1. Com seu protagonista, anote modelo, roupa/props, saúde/colete, dinheiro nativo, banco RPCore e equipamento (inclusive armas que compartilham munição). Crie um cadastro, mude nome/modelo e cancele: deve continuar no protagonista com os dados anteriores; cadastro fica na lista com saldo zero.
2. Confirme o carregamento masculino. Confira mesmo local, perfil novo, saúde padrão, sem armas/colete, saldo zero, inventário/progresso separados. Teste trabalhar/receber pagamento nesse perfil sem mudar o banco/empresa do protagonista.
3. Personalize rosto/cabelo/olhos/maquiagem na barbearia. Salve roupa/equipamento atual, volte ao protagonista e confira sua roupa/props, saúde/colete, armas, acessórios, tintas, pentes, munição compartilhada e dinheiro. Carregue o personagem outra vez: aparência confirmada, corpo e progresso devem voltar sem taxa de aparência.
4. Crie e carregue um personagem feminino. Confira independência de roupas, rosto, inventário e dinheiro entre os dois cadastros. Alternar modelo antes da confirmação não deve substituir ninguém. Voltar ao personagem já ativo não deve reiniciar equipamento/saldo.
5. Teste carregar com atividade reservada, em veículo, andando, procurado ou missão: deve recusar/interromper sem limpar procurado nem retirar você do veículo. Não use missões para forçar a troca.
6. Se surgir Pendente/Retomar volta, repita a confirmação após encerrar o impedimento. Não deve sobrescrever o snapshot antigo com o corpo parcial. Falta de peça/arma de mod deve preservar dados e registrar erro; confirme que o recurso está instalado antes de retomar.
7. Reinicie scripts e abra F10: lista deve continuar, sem trocar modelo automaticamente. Confira F2/F11/J, teleporte, barbearia, guardarroupa/moradia/storage, PDM e trabalhos dos perfis antigos.

Build Release: zero erros/avisos; 1374 testes passaram (42 novos). Cobrem diretório novo sem sobrescrita, IDs/caminhos, modelos/perfis, nomes, recursos/snapshots, munição compartilhada, comparação de restauração, campos desconhecidos, JSON protegido e pendências. Não executam natives nem comprovam a troca visual no jogo. GTA não foi executado nesta nuvem.
