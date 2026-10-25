# RPCore 0.39.0 — fase 6.4A: seguro de veículos pessoais

## Instalação e acesso

Extraia RPCore-update-0.39.0.zip da branch rpcore-entrega. Abra PowerShell na pasta extraída que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Requer GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não reinstala .oiv/.rpf e não executa PSRP.dll junto. Acrescenta Jobs/VehicleInsurance.json quando ausente; conserva configurações/saves anteriores, inclusive personagens, F2 e F11.

Abra **F10 → Seguro veicular / recuperacao**. Requer garagens habilitadas e jogador sem missão/procurado/combate/cutscene/outra interface. Cadastro, localização, recuperação e cancelamento ficam no mesmo menu. Não há tecla nova.

## Cadastro e apólices

Seguro é opcional e o cadastro é gratuito, sem mensalidade ou renovação automática. Veículo precisa ser carro, moto ou quadriciclo já registrado numa garagem própria. Bicicletas, veículos de trabalho e veículos sem registro não entram. Para registrar: compre uma garagem, guarde seu veículo e retire-o pelo sistema existente; a tecla de garagem configurada aparece na ajuda do menu.

Como motorista, pare o veículo sem passageiros/anexos, em condições de rodar, e escolha Cadastrar veiculo atual / gratis. A apólice salva ID do registro, modelo, placa e franquia contratada. Não cria outro registro de carro, compra garagem, apaga veículo ou cobra no cadastro. Cadastro repetido é recusado. A franquia padrão é **$10.000 do banco RPCore por recuperação**, como na referência PSRP; não usa dinheiro em mãos.

Cada apólice permite:

- Localizar veiculo / GPS: se houver um único veículo carregado com modelo/placa correspondentes, marca sua posição atual. Caso contrário, marca a última posição salva com aviso de que não confirma presença. Não entrega nem cria veículo.
- Recuperar destruido: solicita reparo do mesmo veículo no local mediante franquia, nas condições abaixo.
- Cancelar seguro: confirmação que remove somente a apólice, mantendo carro, registro da garagem, combustível e itens do porta-malas. Sem reembolso de franquias anteriores. Pedido pendente desse veículo precisa ser resolvido antes.

Mudar placa/modelo não atualiza a apólice automaticamente. O registro e o veículo precisam corresponder à identidade contratada. Cadastros de outros perfis não são compartilhados. A atualização não cadastra/aplica seguro automaticamente nos carros existentes.

## Recuperar veículo destruído

Saia do veículo e fique a até 30 metros dele. Deve existir um único veículo com a identidade segurada, parado, vazio, desanexado e fora de trabalho. Só aceita veículo morto/considerado destruído ou com motor/tanque inutilizado, não um carro saudável com motor desligado. Veículo guardado ou em remoção pela garagem não pode receber pedido. Sem saldo suficiente, nenhuma mudança no mundo ou cobrança.

Ao escolher Recuperar destruido, grava a franquia e um pedido com o veículo/handle, snapshot e recursos antes de iniciar reparo. Grava autorização de início antes de modificar o veículo. Então repara o mesmo objeto, restaura saúde/motor/carroceria/tanque, retira o efeito nativo de queimado, lava e deixa o motor desligado. Não troca modelo, placa, pintura ou modificações, não cria cópia, não remove carros próximos e não desloca o veículo. Não é serviço de guincho: carro virado/submerso continua no local.

Combustível usa o tanque RPCore como referência quando já existe; senão usa o nível nativo capturado. Não enche o tanque: ao retomar, limita ao saldo de combustível ainda existente. Quilometragem/capacidade do tanque não são resetadas. Óleo nativo é preservado. Após o reparo verifica condições de rodagem, saúde e níveis de combustível/óleo; só depois atualiza o snapshot do registro existente e conclui o pedido, sem segundo débito.

A franquia não vira indenização em dinheiro. Garagem e seguro continuam serviços separados: compra/armazenamento/reparo/lavagem/localização antigos mantêm suas regras. Esta etapa acrescenta recuperação física de destruído no mundo, não substitui a recuperação já existente de carro ausente pela garagem.

## Pendência, retomada e estorno

Falha de save antes de iniciar mantém o pedido não autorizado para retomar ou cancelar/estornar. Estorno devolve a franquia uma vez; se o banco estiver no limite inteiro, mantém pendência até haver espaço. Falha antes de gravar o pedido não cobra nem repara.

Depois da autorização, parte ou todo o reparo pode já ter acontecido. Retomar recuperacao usa o mesmo pedido e não cobra outra vez; exige o veículo com mesmo handle/modelo/placa, único no mundo carregado, próximo/vazio e no mesmo perfil. Se o carro já estiver reparado, conclui sem exigir destruí-lo novamente. Não retoma automaticamente no load.

Depois de autorizado não oferece estorno: há retomada ou Encerrar pedido sem estorno, com confirmação explícita. Encerrar não desfaz reparo nem devolve franquia. Veículo desaparecido/handle diferente não é substituído automaticamente; conserva pedido para tentar retomar ou encerrar conscientemente.

Enquanto existe pedido pendente daquele carro, a garagem bloqueia sua retirada/armazenamento e não sobrescreve seu snapshot no autosave. Outros carros mantêm o comportamento anterior. Isso conserva o registro de recuperação até concluir/estornar/encerrar, sem criar duplicatas. O objeto Transport do perfil continua o mesmo para os módulos que já o utilizam.

## Configuração e referência

Jobs/VehicleInsurance.json: Excess entre 0 e 1.000.000 (padrão 10.000), MaxPolicies entre 1 e 128 (padrão 64). Config inválida usa padrão embutido em memória, preservando o arquivo. Franquia é salva na apólice: mudar Excess só afeta novos cadastros, não pedidos/apólices antigos. Para um teste sem franquia, ajuste Excess=0 com jogo fechado e faça novo cadastro. Reduzir limite não remove apólices existentes; apenas impede novos cadastros acima dele.

Novo Wallet.VehicleInsurance é opcional, com apólices, recibos, pedidos e totais. Save antigo sem ele não é migrado/cobrado. Rollback conserva livro/banco/registro da garagem se gravação falhar. Modelo+placa normalizada e ID do registro garantem correspondência; duplicidade de registro/identidade é recusada. Campos desconhecidos do livro/apólices/pedido e veículo são preservados.

Comparados SaveVehicleToPersistence, menu de gerenciamento, Cancel Insurance e Recover Vehicle em Vehicle_Insurance da PSRP.dll: cadastro/persistência, franquia bancária 10.000 e preservação de combustível/óleo. Código próprio. A referência aceita cadastro direto e move o reparado a uma vaga fixa; RPCore exige propriedade já registrada e repara no local. Não reproduz a exclusão de veículos próximos usada na entrega da referência. InsuranceBanner já está no catálogo/YTD instalado e é usado por fallback em memória, sem reescrever Bindings.json antigo.

Entrega/guincho, substituição de desaparecido pelo seguro, importação automática de apólices PSRP, desgaste de óleo, licenças e demais diferenças continuam pendentes. Fases 8 (interface/mecânicas) e 9 (trabalhos) mantidas. Não se declara paridade completa com todo Vehicle_Insurance, que também contém combustível e outros recursos.

## Testes dentro do GTA

1. Tenha uma garagem própria, um carro/moto registrado e pelo menos $10.000 no banco RPCore. Retire o veículo, pare como motorista e cadastre no novo menu. Não deve cobrar nem duplicar o registro. Repita cadastro: deve recusar duplicata. Veículo de rua não registrado ou de trabalho deve ser recusado.
2. Localize por GPS, confira modelo/placa e cancele a apólice. Carro, garagem, combustível e itens devem continuar. Cadastre novamente antes do teste de destruição.
3. Com o carro segurado destruído e sem procurado/missão, saia e fique próximo. Recupere: cobra uma franquia, repara o mesmo carro no local, conserva modificações/placa/pintura e recursos, motor fica desligado. Confira gasolina do RPCore, óleo, quilometragem, pneus/portas/vidros e funcionamento após entrar.
4. Teste carro saudável, ocupado, longe, anexado, guardado ou sem saldo: deve recusar sem cobrar. Não deve limpar procurado, retirar NPCs, criar outro carro ou apagar entidades.
5. Caso surja pendência, retome perto do mesmo carro, sem pagar de novo. Se já foi reparado, deve permitir concluir. Garagem deve bloquear apenas esse carro enquanto pendente; outros veículos seguem normais. Não é necessário provocar falha de disco: rollback/replay têm testes automáticos.
6. Reinicie scripts: apólice/pedido continuam, sem cobrança ou reparo automático. Mude perfil: apólices e dinheiro não devem vazar. Confira garagens/loja/reparo/lavagem antigos, combustível/porta-malas, personagens e volta ao protagonista, moradia, F2/F11/J, PDM e trabalhos.

Build Release sem erros/avisos; 1412 testes passaram (38 novos). Auditoria PSRP-SYSTEMS-AUDIT-0.39.0.json e VALIDACAO.md registram limites. GTA não executado nesta nuvem: reparo de carcaça queimada, recursos, banner e convivência com mods precisam de teste real.
