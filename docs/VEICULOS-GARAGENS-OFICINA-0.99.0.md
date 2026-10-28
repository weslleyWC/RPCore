# Refinamento0.99 — veículos, garagens e oficina

Referência consultada: DrivingFeatures (motor, faróis, luz interna, vidros, portas, piloto automático, setas, alerta e neon), RadialMenu (páginas veiculares) e os fluxos de personalização em SettingsMenu. Código próprio integrado aos carros e pagamentos existentes. Não executamos PSRP.dll nem GTA neste ambiente.

## Controles e radial

J → Vehicle Control agora abre páginas de Motor/condução, Luzes, Portas e Vidros. Elas usam o mesmo controlador do menu K/atalho personalizado e do F11 → Vehicle Settings. Voltar funciona por setor, Backspace, botão direito ou B; Menu completo abre o menu nativo com estados/ações. As teclas existentes não são regravadas. Cinto chama o SeatbeltSystem atual, sem acionar ao mesmo tempo outro sistema com tecla igual.

- Motor liga/desliga com a verificação de combustível existente; não dá combustível nem muda potência/handling.
- Cinto mantém sua mecânica atual. O piloto automático mantém a velocidade-alvo por aceleração limitada, nunca por SetForwardSpeed/multiplicador; freio, acelerador, colisão, pouca velocidade, perda de solo, menus ou contexto inseguro cancelam. Não substitui o trabalho físico de dirigir e não é autopiloto de rota.
- Faróis, luz interna, setas e alerta mostram o estado atual no menu. O override de faróis é devolvido ao modo natural ao deixar o controle; setas restauram o estado anterior conforme o mecanismo existente.
- Capô, porta-malas e quatro portas exigem carro parado (até0,5m/s), porta existente e sem avaria. Portas e vidros não são oferecidos em motos.
- Quatro vidros têm alternância por sessão e comandos explícitos Subir todos/Baixar todos; o menu indica a próxima ação. A referência usa IsIntact para decidir subir/baixar, o que não é um sensor universal de posição. Aqui não se trata vidro quebrado como posição e não se chama FixWindow. A alternância acompanha comandos do RPCore; se outro mod mudar o vidro, use Subir/Baixar para sincronizar. Modelos sem o bone padrão de vidro são tratados conservadoramente.
- Neon liga/desliga somente lados observados/registrados, sem instalar novos lados gratuitamente. Antes de apagar, a prova de equipamento é gravada em Transport.NeonEquipment, dicionário opcional por identidade model/plate do veículo. Assim pode religar depois do reload sem pagar de novo. Falha normal na gravação conserva o dicionário anterior e não apaga. Registro inválido é conservado e recusa alteração; equipamento nunca observado não é presumido instalado.

Menus são vinculados ao motorista/ped/perfil/carro; sair, trocar de carro/perfil, perder controle, morrer, entrar em missão ou cutscene encerra o contexto. Ações enfileiradas do radial ainda conferem o mesmo carro antes de executar, além das proteções anteriores de ator/perfil/tempo. Passageiro não opera motor/portas/alvos alheios; o cinto continua validado pelo sistema próprio. Não atua sobre tráfego próximo nem busca um carro vizinho para usar como alvo. F11/GPS chamam veículo/garagem/concessionária diretamente, evitando conflitos de tecla.

## Oficina

A raiz deixa de misturar dezenas de slots: há grupos de desempenho, carroceria/peças, rodas, pintura/interior/perolado, vidros/placa/livery, luzes e extras. Nomes de categorias de rodas/cores/vidros/placas usam enums disponíveis, em vez de só números onde possível. Itens incompatíveis continuam verificados pelos natives e não recebem cobrança se a instalação não for confirmada.

Primária e secundária aceitam RGB personalizado por$300; neon e fumaça por$200. Digite três canais inteiros de0–255, por exemplo255,128,0; valores inválidos/cancelamento não compram. Os oito presets anteriores continuam disponíveis. Alterar seleção não muda o carro; confirmação usa o preço, verificação e rollback existentes. Nenhum editor RGB grátis foi acrescentado ao radial para contornar a oficina.

Mudança/reconstrução de página ocorre depois do callback de ativação, evitando modificar a lista que LemonUI está percorrendo. Mantém seleção na atualização da mesma página, retorna ao grupo pai e protege a ativação por botão mantido. Back/Escape/B tenta voltar ao pai; existe também a linha Voltar. Fechar na raiz libera o gate. Pausa, cutscene, perda de controle, carro/ator/perfil, passageiros, trabalho, seguro pendente, movimento ou saída do local encerram/bloqueiam as ações. Ponto de oficina do editor precisa continuar ativo.

## Garagens

Custos, capacidades, locais e identidades anteriores permanecem. Garagens com preço0 explicitamente configurado podem ser adquiridas sem tentar debitar0. Veículos de trabalho, ocupados, rebocados/anexados, de missão não autorizada e com operação pendente continuam protegidos.

Ao guardar, o snapshot durável continua sendo escrito antes de pedir a remoção. A confirmação agora consulta o handle original e identidade/modelo, em vez de confiar no wrapper que Delete pode invalidar. Se o carro permanece, o registro fica fora da garagem e pode ser localizado; não é tratado como removido apenas porque o wrapper perdeu o handle. A mesma pessoa/perfil deve completar a saída/armazenamento.

Ao retirar/localizar, resolve o registro atual por ID/garagem: um menu aberto antes do autosave não usa equipamento/combustível antigo. Depois do carregamento do modelo, revalida ator/perfil, local/propriedade e espaço livre de carros/pedestres. Se o contexto mudou ou a saída foi ocupada, não cria o veículo; não limpa a área nem move outros recursos. Modelo indisponível conserva o registro. O mecanismo de localizar um carro já no mundo continua impedindo cópias.

Reparo/lavagem atualizam imediatamente o snapshot do carro pessoal registrado antes de salvar a carteira; não dependem do próximo autosave. Combustível e metadados são mantidos. Falha normal devolve dinheiro e restaura registro/estado capturado. Snapshot não descreve cada vértice de deformação: um erro após Repair pode não reconstruir exatamente a deformação visual anterior. Não há transação de crash entre carro nativo e JSON, nem promessa de recuperação perfeita desse intervalo. Custos continuam$500/$25, sem reequilíbrio de trabalhos.

## Teste GTA

3811 testes aprovados (3772 anteriores +39 novos), Release net48 sem erros/avisos. Novos testes cobrem índices de portas/vidros, contexto de proprietário, carro parado, RGB, identidade após remoção, registro atualizado, prova durável de neon/falhas/metadata e rollback de compra RGB. Natives, geometria, streaming e LemonUI precisam destas verificações:

1. Em um carro pessoal, teste J → Vehicle Control e menu K/F11: motor, cinto, piloto, faróis, luz interna, setas e alerta. Confira painel/estados, cancelamento do piloto e comandos habituais.
2. Pare: abra/feche cada porta, capô/porta-malas; alterne vidros e use Subir/Baixar todos. Em movimento, em moto e em modelos com portas/vidros ausentes ou avariados, confirme recusa sem dano/reparo indevido.
3. Instale lados de neon na oficina. Apague, recarregue os plugins e religue: só os lados comprovados devem voltar. Carro sem neon não deve ganhar instalação gratuitamente.
4. Troque de carro/ped/perfil com menu/radial aberto ou imediatamente após selecionar: o comando antigo não deve atingir o carro novo. Teste motorista versus passageiro, missão/pausa/cutscene e saída do carro.
5. Na oficina, percorra grupos, volte por B/Escape/linha Voltar, compre uma peça e mantenha o botão para confirmar que não cobra repetidamente. Teste presets e RGB digitado em primária/secundária/neon/fumaça; cancele e digite valores inválidos. Saldo/peça antiga permanecem quando não há confirmação.
6. Guarde/retire o carro e reinicie os plugins: confira cores RGB, peças, rodas, extras, combustível e danos. Saída ocupada deve recusar sem apagar/mover carros/peds. Confira carro já no mundo sendo localizado sem cópia.
7. Repare/lave, recarregue em seguida e confira snapshot/combustível/saldo. Teste oficina do editor ativa/removida e carro de trabalho recusado para armazenamento/tuning/serviço indevido.
8. Regressão: F2/banco/F10, F11/teleporte, GPS/marcadores, inventário/peso/munição/lojas/sono. Física de direção, estacionamento assistido, todas as variantes de tuning da fonte e o refinamento completo dos trabalhos ainda não são declarados concluídos; veja COBERTURA-REFINAMENTO.md.
