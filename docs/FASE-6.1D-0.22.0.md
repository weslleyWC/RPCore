# Fase 6.1D — RPCore 0.22.0: coleta de valores PDM

Continuação da empresa: carro-forte e segurança coletam parte do caixa e transferem ao banco pessoal após entrega/compensação. Mantém caixa, estoque, funcionários, coleta de carros, exposição e entregas anteriores. Não declara toda a PDM concluída. Fases 8 (interface/mecânicas) e 9 (refinamento de todos os trabalhos) permanecem.

## Instalação

Feche o GTA, extraia RPCore-update-0.22.0.zip e abra o PowerShell **na pasta onde está Install-RPCore.ps1**:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Use seu caminho real. DLL pronta; instalador faz backup da anterior, preserva saves/configurações e acrescenta Jobs/PdmSafe.json só se ausente. Não apague a carteira. F2/celular, F11/teleporte e recursos OpenIV já instalados permanecem. Não precisa reinstalar OIV/YTD/RPF/DLC.

## Roteiro principal no GTA

1. **F10 → Trabalhos → PDM / empresa e entregas → Coleta de valores / carro-forte**. Também há entrada **PDM / coleta de valores** no menu de trabalhos.
2. Empresa precisa ser sua. Vá a pé ao escritório externo, sem missão/procurado/combate e sem entrega/coleta da empresa pendente. Encerre outro trabalho físico antes de solicitar. Tenha dinheiro **no caixa empresarial**, não apenas no banco pessoal.
3. Escolha **Solicitar coleta: 1000**, 10000, 50000 ou saldo, até um milhão. O valor sai do caixa disponível e fica **em coleta**. Ainda não aparece no banco. Exemplo: caixa 10000 → disponível 9000 + reservado 1000; banco fica igual.
4. Aguarde o **Stockade azul** chegar à frente da PDM. O segurança sai e caminha até a entrada externa, com a maleta. Não precisa entrar no carro-forte nem dirigir. Não há teleporte forçado para resolver trânsito.
5. A pé, desarmado e perto do segurança amarelo, pressione **E** para passar os valores. Aguarde três segundos sem se afastar, abrir menus ou interromper. O segurança deve estar a pé/próximo da entrada e o carro-forte parado. Antes dessa confirmação, os valores ainda podem ser devolvidos ao caixa.
6. Depois da mensagem de entrega, o segurança volta ao Stockade e sai em direção à rota bancária. Aguarde **dez segundos ativos de compensação após a saída**. Abrir menu/pausa reinicia o intervalo elegível; feche e aguarde novamente.
7. O banco pessoal recebe exatamente o reservado. No exemplo, banco anterior +1000, caixa 9000, em coleta zero. Não paga bônus, não conta como venda nem lucro novo: é transferência de dinheiro da própria empresa. Confira que repetir/recarregar não credita novamente.
8. Se o embarque/saída ficar bloqueado, libere o carro-forte ou abra **Recuperar / concluir coleta pendente**. Depois da entrega confirmada, essa ação conclui a compensação com o mesmo ID e não devolve também ao caixa.

A rota de saída usa o banco da referência, mas a compensação não exige acompanhar um trajeto inteiro até ele. É uma coleta com saída e prazo, conforme o fluxo estudado, sem simulação bancária externa. O saque direto anterior continua disponível como alternativa compatível; não foi removido.

## Cancelamento, falhas e recuperação

**Antes de entregar a maleta:** segure End por um segundo fora dos menus, ou escolha Cancelar antes da entrega. Pare e saia do carro-forte se tiver entrado. O próprio segurança tenta sair/parar antes da limpeza; não apaga o veículo com jogador/outros ocupantes dentro. Depois, o reservado volta ao caixa, sem banco ou recompensa. Morte, procurado, missão, perda do segurança/carro-forte ou prazo padrão de 120 segundos antes da entrega solicitam a mesma devolução. Se o carro-forte não conseguir chegar, cancela/devolve em vez de teletransportá-lo através de obstáculos.

**Depois da entrega confirmada:** não permite cancelamento com reembolso. O dinheiro fica pendente de compensação mesmo se o script/mundo interromper, o segurança morrer ou o veículo sumir. Recuperar conclui no banco uma vez. Isso protege valores já entregues sem criar um segundo saldo no caixa.

Se o banco estiver no limite, conserva a transferência entregue e tenta novamente; use o sistema bancário existente para liberar espaço e Recuperar no menu. Não descarta valores por overflow. Falha de gravação conserva o snapshot anterior e tenta de novo; mudanças financeiras são checkpoint único.

Ao recarregar, reconhece a van por handle/modelo/placa exclusiva. Segurança é reconhecido por marcador ou identidade lembrada associada à van autenticada; maleta por marcador ou vínculo ao segurança autenticado. Toca somente nos recursos reconhecidos. Veículo ocupado/andando mantém pendência antes do reembolso: pare/saia e use Recuperar. Mundo reiniciado sem temporários: antes da entrega, devolve; depois da entrega, compensa. Não cria automaticamente outra coleta sobre uma pendente.

O valor reservado também mantém espaço no limite do caixa para uma devolução futura. Não é dinheiro disponível para comprar estoque, pagar pessoal ou sacar duas vezes. Gestão/movimentação da empresa pelo escritório espera concluir a coleta; salários continuam acumulando normalmente, mas não consomem a reserva. Outros trabalhos físicos usam o mesmo controle de turno; streamer permanece paralelo.

## Persistência e implementação

Campo aditivo **Wallet.json/Pdm/SafeTransfer**: ID, valor, Handed, identidade da van, segurança/maleta e token. Campo ausente significa nenhuma coleta, sem cobrança retroativa. Não cria um novo saldo de cofre nem muda o significado do caixa anterior.

- Reserva: caixa disponível diminui e registro é criado no mesmo salvamento.
- Entrega: grava Handed antes de embarcar/partir.
- Compensação: banco pessoal aumenta, Withdrawn aumenta e registro fecha juntos; Revenue/Sold ficam iguais.
- Devolução: caixa aumenta e registro fecha juntos, somente se não houve entrega.
- Recibos recentes e restauração de snapshot seguem a arquitetura existente. Replay de compensação anterior não usa valor de uma coleta nova.

PdmSafeData.cs contém opções, transações e estados puros; PdmSafeSystem.cs contém NPC, carro-forte/maleta, menus, animação, rotas, cancelamento/recuperação. PdmData.cs/Wallet.cs/Main.cs/PdmCompany.cs integram o fluxo. PdmLogistics/Data e PdmStaffData bloqueiam início/gestão incompatíveis durante a reserva, sem mudar os ciclos anteriores.

## Referência e diferenças

Estudo de **PSRP/PDMCompany.cs/HandleSafeEvent**, retiradas e SafeManagement. Pontos conferidos: Stockade (-53.67139, -994.5934, 28.82287), parada (-77.51719, -1110.635, 25.44406), segurança/entrega (-63.2041, -1092.664, 26.53896), rota bancária (1182.303, 2646.111, 37.81576). Modelos stockade, s_m_m_armoured_01 e prop_security_case_01; gesto mp_common/givetake1_a/b. Spawn procura vaga segura e pode ajustar posição/chão; não apaga ambiente para abrir espaço.

Implementação própria. A referência mantém valor a coletar e debita o caixa na conclusão; RPCore reserva antes para impedir gasto duplicado. A referência usa warps após timeout e segurança invencível; RPCore espera/cancela antes da entrega, sem teleporte de veículo/NPC, sem invencibilidade e sem alterar relações/IA policial global. Usa limpeza somente dos seus recursos e animação/clipes iniciados por este sistema.

Escopo desta subfase é **coleta física de valores existentes**. Ainda não implementa cofre 3D interno/abrir-fechar/roubo/seguro, financiamento/repossessão, aluguel, showroom inteiro, demais funcionários/IA e transporte de cinco carros/propriedade dos transportadores. São próximas subentregas PDM; refinamento global continua nas fases 8/9.

## Verificação adicional no GTA

Teste ciclo de 1000 e saldo total, repetir sem duplicar, cancelar durante chegada/caminhada, interromper o gesto com menu/distância/arma, recarregar antes/depois da entrega, ocupar o carro-forte e liberar depois, chegada bloqueada e Recuperar após embarque bloqueado. Confira retirada direta anterior, salários/pagamentos, estoque/coleta/exposição/venda, dinheiro por perfil, garagem rejeitando o Stockade temporário, F2 e F11. Não há novo atalho substituindo os existentes.

Build net48 com Enhanced 1.1.0.6/LemonUI 2.2 sem erros/avisos; **824 testes aprovados**, incluindo 41 novos de reserva/transferência/reembolso/limites/replay/rollback/integração/tempo. Auditoria estática e instalador simulado não executam GTA, trânsito, animações ou navegação real. Sua validação do percurso físico continua necessária.
