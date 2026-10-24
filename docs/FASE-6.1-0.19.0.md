# Fase 6.1A — RPCore 0.19.0: núcleo da empresa PDM

O usuário confirmou a arrancada funcionando e pediu avançar. Esta entrega começa a fase 6 com o núcleo comercial da PDM: aquisição, caixa empresarial separado, aporte/saque, estoque pago, contrato de entrega física, cliente/chaves/recebimento, danos, reparo e recuperação. A concessionária RPCore Motors e as garagens pessoais continuam independentes.

A classe PDMCompany da referência tem mais de 11 mil linhas e diversos sistemas. Não se declara a empresa inteira concluída. As próximas subentregas da PDM incluem logística/coleta no porto e showroom, equipe/salários/infraestrutura e financiamento/repossessão/aluguel/cofre, antes de avançar aos demais sistemas da fase 6. Fase 8 continua reservada a interface e mecânicas do HUD/menus; fase 9, ao refinamento de todos os trabalhos com estudo aprofundado da PSRP.dll.

## Instalação

Feche o GTA, extraia RPCore-update-0.19.0.zip e execute da pasta extraída com seu caminho real:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

DLL pronta. Backup da anterior; Jobs/Pdm.json acrescentado só se ausente. Não apague saves/configurações. Mantém F2/celular, F11/teleporte, veículos pessoais, inventário/bolsas, licenças, progressos e recursos instalados. Sem reinstalar/editar OIV/YTD/RPF/DLC/dlclist.

## Roteiro rápido

Para o primeiro ciclo padrão, tenha **35000 no banco pessoal**: 25000 para adquirir a empresa e 10000 de capital. Dinheiro na mão pode ser depositado pelo sistema bancário já existente. Não há empréstimo automático nem dinheiro de teste concedido por esta atualização.

1. **F10 → Trabalhos / contratos → PDM / empresa e entregas → GPS: escritorio externo PDM**. F11/J → GPS também inclui a empresa. Escritório público em **(-63.2041, -1092.664, 26.53896)**, ao lado da PDM. Não precisa acessar interior.
2. A pé no escritório, sem procurado/missão/combate, escolha **Adquirir empresa: 25000**. Debita o banco uma única vez por perfil; aquisição não inclui caixa nem carros.
3. Entre em **Caixa / aportar / sacar → Aportar 10000**. Transfere do banco pessoal para a empresa. O caixa fica com 10000.
4. **Comprar estoque / fornecedor → Asea por 8000**. A compra usa o caixa da empresa, que fica com 2000. O veículo fica registrado no estoque virtual, com ID/placa próprios, custo, preço e condição. A coleta física no porto ainda não faz parte desta entrega.
5. **Estoque / entregas / reparar → Asea → Entregar ao cliente**. Precisa estar no escritório e não ter outro trabalho físico ativo. Cria o carro pago na vaga livre de retirada, com blip azul, e marca um cliente amarelo entre dez destinos da referência.
6. Entre **nesse carro da empresa**, dirija ao cliente e pare perto dele. Ao se aproximar, o cliente surge em uma posição pública segura próxima ao endereço; GPS/blip atualizam para o ponto acessível.
7. **Saia do carro, guarde a arma, aproxime-se do cliente e pressione E** para passar as chaves. O carro deve estar vazio/parado e a menos de dez metros do cliente; jogador a menos de três. Espere três segundos sem se afastar/abrir menus.
8. Sem danos, a venda coloca **11200 no caixa da empresa**, retira esse carro do estoque e encerra o contrato no mesmo salvamento. O cliente tenta entrar e sair com o veículo; só então libera normalmente o turno. Se o embarque não completar em 20 s, limpa os recursos próprios sem repetir ou desfazer a venda já confirmada.
9. Volte ao escritório e use **Caixa → Sacar saldo**. No exemplo, havia 2000 mais 11200 da venda: **13200** podem ir ao seu banco pessoal. O lucro operacional desse carro foi 3200; a empresa continua adquirida.

## Banco, caixa e estoque

| Operação | Banco pessoal | Caixa da empresa | Estoque |
| --- | --- | --- | --- |
| Adquirir empresa | -25000 | Sem capital automático | Vazio |
| Aportar 10000 | -10000 | +10000 | Igual |
| Comprar Asea | Igual | -8000 | +1 Asea |
| Entregar/vender Asea sem danos | Igual | +11200 | Remove esse Asea |
| Sacar 13200 | +13200 | -13200 | Igual |

Sem danos, usando banco inicial de 35000, o ciclo completo termina com banco 13200, caixa zero e empresa adquirida. Não soma aquisição da empresa como receita nem deposita a venda duas vezes. Saque é transferência do saldo existente, não um segundo pagamento. A conquista cash_earned usa margem positiva da venda, não aporte/saque.

Catálogo inicial próprio do RPCore:

| Veículo | Custo empresarial | Venda sem danos |
| --- | --- | --- |
| Declasse Asea | 8000 | 11200 |
| Dinka Blista | 12000 | 16800 |
| Vapid Dominator | 30000 | 42000 |

Capacidade: seis carros. Um contrato por vez. Não compra/repara estoque durante entrega pendente. Valores/catálogo são configuráveis. O preço de aquisição da referência é 3200000; o RPCore usa 25000 neste ciclo inicial da sua economia. Não é afirmado preço idêntico ao PSRP.

## Danos, reparo e cancelamento

Condição de motor/carroceria por unidade (0–1000). Venda usa o pior valor de carroceria entre o salvo e o atual: preço base multiplicado por **75% + até 25% conforme condição**. Carroceria 500 dá 87,5% do preço base: Asea rende 9800. Motor inferior a 100 não conclui venda; inferior a 300 ou carroceria inferior a 300 exige reparo antes de começar outra entrega.

**Reparar no estoque: 1000** debita o caixa e restaura motor/carroceria do item empresarial. Não repara carros pessoais, não é cobrado de novo se já estiver íntegro e não funciona durante contrato ativo. Combustível e controles existentes continuam aplicados normalmente; não há abastecimento grátis nem alteração desses sistemas.

End segurado por um segundo ou Encerrar entrega solicita cancelamento. **Pare e saia do carro da empresa** para devolvê-lo ao estoque. Não apaga veículo ocupado nem libera outra entrega enquanto a devolução estiver pendente. O carro retorna com danos, sem venda/pagamento; o custo de compra não é devolvido porque o bem continua sendo da empresa. Falha ao salvar mantém a devolução pendente para retry. Danos são gravados antes de remover o veículo, para que uma falha posterior não restaure um carro novo indevidamente.

Morte, procurado, missão, carro destruído/indisponível, comprador morto ou prazo de dez minutos solicitam a mesma devolução. Se houver ocupantes, espera liberar o veículo. Carro sumido/destruído pode voltar ao estoque danificado e exigir reparo. Abrir menu/pausa durante a passagem das chaves interrompe o gesto e exige E novamente; ainda não retira o item nem paga. A venda só se confirma ao completar o tempo elegível e o checkpoint.

Não se aplica um cancelamento de estoque depois de uma venda já paga: o estado passa a concluído e apenas aguarda a saída/limpeza do cliente. Caso haja limite de caixa/contadores, não vende nem remove estoque; notifica a pendência e permite cancelar/devolver para resolver o caixa. Erro de gravação repete com o mesmo ID, sem crédito duplicado.

## Persistência e recuperação

Campo aditivo **Wallet.json/Pdm**: Owned, AcquisitionPrice, Cash, Invested, Withdrawn, StockSpent, SoldCost, Revenue, Repairs, Bought, Sold, Stock, Active, Ledger e Receipts. Caixa/banco nas transferências e estoque/receita na venda são checkpoint único, com restauração de referência/saldo em exceção. Histórico de caixa limitado a 64 entradas; recibos recentes a 512.

Cada unidade paga guarda ID, modelo/nome, placa, custo/preço de compra do catálogo daquele momento e condição. Mudar o catálogo depois não reescreve o preço de um carro já comprado. Venda confirma a operação ativa correspondente antes de remover o bem. Nunca usa um carro pessoal como se fosse o veículo empresarial.

Active guarda ID do contrato/unidade/destino e handle do veículo físico. A recuperação compara **handle + modelo + placa** com o registro antes de tocar no veículo lembrado. Se esse carro ainda estiver ocupado ou andando, conserva a pendência e pede para parar/sair; use Recuperar entrega interrompida no menu. Depois remove só o temporário correspondente e devolve a unidade ao estoque. Se o mundo foi reiniciado e o temporário não existe mais, conserva a unidade paga e limpa o contrato interrompido. Não cria automaticamente outro carro enquanto o contrato anterior estiver pendente.

Autosave/encerramento lembram danos sem concluir venda. Em troca de perfil, não escreve no perfil novo usando o contrato antigo: limpa/libera apenas recursos próprios e recupera as pendências do perfil carregado. Veículo próprio ocupado não é apagado no cleanup; usa o controle de veículos de trabalho para impedir armazenamento indevido na garagem pessoal. Garagens, compra pessoal na RPCore Motors, Transport, inventários, empresas/campos desconhecidos e licenças anteriores são preservados. Não converte dados financeiros legados da PSRP nesta subentrega.

## Referência, recursos e limites

Inspeção estática de **PSRP/PDMCompany.cs**: compra da empresa, PDMBusinessBankCurrency, estoque, pedidos, entrega a domicílio/passagem de chaves/cliente levando carro, danos e sistemas adicionais. Os dez endereços e a retirada **(-25.2498, -1082.469, 25.97876)** foram conferidos nessa classe. O escritório externo reutiliza o ponto do NPC de coleta do cofre da referência, como adaptação do RPCore para evitar dependência de interior/portas.

Implementação própria. Banner PSRPBANNER2 dos YTD existentes; a referência também usa o banner nativo PDM. Animação de chaves mp_common/givetake1_a. Não abre portas à força, não elimina geradores/carros do showroom/ambiente nem altera relações de NPCs. Spawn procura vaga com limites orientados e colisão/chão verificados, sem mover outros veículos. Cliente é próprio, sem invencibilidade. Ajusta o ponto final para uma posição de pedestre segura próxima ao destino; não afirma que a posição nativa ajustada é idêntica à coordenada bruta.

Esta entrega tem estoque virtual comprado e entrega unitária. **Ainda não implementa** transporte/coleta no porto, estoque físico completo de showroom, equipe/salários, anúncios/upgrades/segurança, vendedores de usados, financiamento/repossessão, aluguel, caminhões/carretas ou logística do cofre. São próximas subentregas da PDM dentro da fase 6; o refinamento global de todos os trabalhos permanece na fase 9.

**Jobs/Pdm.json** controla aquisição, capacidade, reparo, tempo de chaves/prazo, escritório/retirada, catálogo/preços e clientes. Criado/instalado só se ausente e embutido como fallback. Configuração válida mantém overrides; inválida fica no disco e usa defaults. Estado semanticamente inválido é preservado, sem zerar caixa/estoque.

## Arquivos alterados

- PdmData.cs/PdmCompany.cs/config/Jobs/Pdm.json: empresa/caixa/estoque, transações/ledger, entrega/NPC/chaves/saída, danos/reparo e recuperação.
- Economy.cs/Wallet.cs: campo Pdm e adaptadores de checkpoint aditivos, preservando Transport e os demais campos.
- Main.cs/SettingsSystem.cs: 15ª entrada na central, 14ª atividade física exclusiva e GPS da empresa. Streamer permanece paralelo.
- AchievementSystem.cs: aquisição e venda reais; cash_earned só na margem positiva da venda.
- Visuals.cs/config/Visuals/Bindings.json: banner da empresa/fallback com bindings mesclados em memória.
- RPCore.csproj/tests/RPCore.Tests.csproj: padrão embutido e lógica real nos testes; tests/PdmTests.cs: 41 casos novos.
- AssemblyInfo.cs/Main.cs/CoreMenu.cs/VisualAssetDefaults.cs: versão 0.19.0; auditoria, documentação e pacotes atualizados. DealershipSystem.cs/GarageSystem.cs não foram alterados.

## Validação e teste no GTA

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros/avisos. **694 testes aprovados** (653 anteriores e 41 novos): aquisição/capital/saque, estoque/capacidade/placas, contrato/receita/cancelamento/danos/reparo, saldo/limites, replay/rollback/reload, prévia de desconto/preço imutável, unknowns e carros pessoais/licenças/atividades anteriores, tempo de chaves/interrupt/retry/relógio com overflow. Não executam GTA/IA/spawn/handles reais.

1. Confira veículo pessoal, banco/bolsas, F2 e saves anteriores. Adquira a empresa, aporte 10000 e compre Asea. Recarregue e confira propriedade/caixa/estoque.
2. Inicie entrega, confira retirada sem sobrepor carros e que a garagem pessoal não aceita o carro empresarial. Vá ao cliente, pare, saia desarmado, E e aguarde. Confira entrada/saída do NPC.
3. Compare caixa antes/depois, item removido uma vez, banco pessoal sem crédito direto e saque posterior correto. Reabrir/recarregar não deve pagar outra venda.
4. Cancele uma entrega, pare/saia e confira retorno ao estoque com danos/sem pagamento. Repare e confira débito único. Teste reload/perfil com entrega pendente e recuperação, sem novo spawn enquanto carro anterior estiver ocupado.
5. Faça uma entrega com dano e compare desconto. Teste falta de capital/estoque cheio, interrupção do gesto e regressão de garagens/concessionária pessoal/trabalhos/streamer/teleporte.

O GTA não foi executado nesta nuvem. Escritório, vaga, cliente/calçada, chaves/embarque/saída, recuperação do veículo real e convivência com recursos instalados precisam do seu teste.
