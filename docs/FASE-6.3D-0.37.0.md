# RPCore 0.37.0 — fase 6.3D: herança e traços faciais

## Instalação e acesso

Extraia RPCore-update-0.37.0.zip da branch rpcore-entrega. Abra PowerShell na pasta extraída que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Requer GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Mantém os saves/configs, incluindo Price personalizado em Jobs/Grooming.json e celular F2. Não precisa reinstalar .oiv/.rpf. Não execute PSRP.dll junto: foi usada apenas como referência estática.

Perto de uma das sete barbearias, a pé, sem procurado/missão/atividade: **F10 → Barbearias / cortes e olhos → Aparencia / rosto e acabamento → Configurar novos valores**. Esta é a nova identificação do menu de acabamento da 0.36. Use **Heranca facial** ou **Tracos faciais**. Os grupos de tintura/barba/pele/maquiagem continuam no mesmo atendimento.

Exige freemode masculino/feminino já carregado com rosto pronto. Não cria ped, não troca o protagonista, não seleciona outro perfil e não altera aparência ao carregar. Criar/trocar personagem ainda é uma etapa posterior; não é necessário fazê-lo para atualizar o mod.

## Herança

Abra Heranca facial e marque Alterar heranca: SIM. Escolha quatro origens independentes: formato A/B e pele A/B. São índices do catálogo nativo do GTA instalado, incluindo os quatro grupos base/DLC; não há limite fixo adivinhado de 45. Mistura formato e Mistura pele variam de 0 a 100%, em passos de 5%: zero favorece A, cem favorece B. Duas origens iguais também são aceitas. Esta implementação usa dois contribuintes; terceiro contribuinte e sua mistura ficam zero.

Se houver uma herança já confirmada no RPCore para esse perfil/modelo, começa pelos valores registrados. Caso contrário oferece as duas primeiras origens disponíveis e misturas de 50% como NOVOS valores. Não os apresenta como leitura da herança atual. Índices indisponíveis não são aplicados; editar a origem permite escolher um índice presente no GTA atual, sem regravar registros antigos automaticamente.

Desativar a herança retira a seleção do rascunho e conserva o rosto atual. Voltar ao atendimento mantém a escolha; só Confirmar aplicacao inicia a mudança. Escolher herança substitui explicitamente a base do rosto e da pele: não é uma operação para manter a composição genética anterior. O resultado da recomposição e sua convivência com detalhes de outros scripts precisam ser conferidos no GTA.

## Vinte traços independentes

No menu Tracos faciais, abra um traço e ative Alterar este traco: SIM. Ajuste de -100 a +100, em passos de 10. Zero escolhe valor neutro novo. O valor inicial é o último confirmado pelo RPCore para o traço/modelo ou zero quando desconhecido; não há getter completo nem leitura por offsets para capturar o morph atual.

| Índices | Ajustes |
| --- | --- |
| 0–5 | Largura, altura, comprimento, ponte, ponta e desvio do nariz |
| 6–7 | Altura e profundidade das sobrancelhas |
| 8–10 | Altura/largura das maçãs do rosto e volume das bochechas |
| 11–12 | Abertura dos olhos e espessura dos lábios |
| 13–14 | Largura e formato da mandíbula |
| 15–18 | Altura, comprimento, largura e cova do queixo |
| 19 | Largura do pescoço |

Cada índice é opcional. Alterar somente o nariz, com herança desativada, não chama o setter de head blend nem setters de outros traços. Desmarcar um traço descarta apenas sua seleção; não o zera e não remove seu registro anterior. Voltar à lista/atendimento mantém escolhas até confirmar ou cancelar.

## Confirmação, carregamento e pendências

Todos os grupos começam desativados em um novo rascunho, inclusive herança e traços já conhecidos. Nenhum setter de aparência é chamado durante seleção. Cancelar, Esc/fechar, afastar, trocar perfil/modelo ou abrir outra interface descarta a seleção e libera a atividade sem cobrar.

O mesmo atendimento continua cobrando **$120 pelo conjunto**, conforme Price existente em Jobs/Grooming.json; não há taxa extra por traço/herança. Pode combinar rosto, tintura, barba e maquiagem em um pedido. Sem grupo válido não cobra. Se todos os valores selecionados já estiverem registrados, a aplicação é gratuita. Reaplicar acabamento confirmado continua gratuito e agora inclui os grupos faciais previamente confirmados.

Confirmação paga grava taxa/pedido; autorização de início é gravada ANTES dos setters. Quando há herança, o mod solicita head blend e aguarda sua conclusão em ticks, a partir do próximo tick, com limite de dez segundos. Não bloqueia a thread, congela jogador, força câmera ou espera dentro de um loop. A atividade permanece reservada durante a espera. Depois aplica os traços escolhidos, cabelo/overlays escolhidos e conclui o registro. Sem herança, traços/acabamento continuam por aplicação direta após confirmação.

Interromper depois da autorização pode deixar parte do visual aplicada. O pedido pago fica salvo para Retomar atendimento pendente no mesmo modelo, sem segunda cobrança. O mod não promete desfazer o rosto anterior desconhecido. Estorno continua disponível somente antes da autorização; depois há retomada ou encerramento explícito sem estorno. Timeout, falha de gravação ou troca de perfil não conclui o pedido em outro personagem. Reaplicação gratuita interrompida mantém o registro confirmado para tentar novamente. Nada é retomado automaticamente no load.

## Compatibilidade e comparação

FaceData.cs define validação de herança/morphs, catálogo de origens, faixas e espera. FaceEditor.cs integra seleção/aplicação ao GroomingSystem existente. Wallet.Grooming.Look recebe Heritage opcional e Morphs vazios por padrão: looks/pendências da 0.35/0.36 continuam legíveis sem migração, cobrança ou seleção automática. Merge conserva grupos/campos desconhecidos, inclusive metadados de herança/morphs já existentes. Barber/guardarroupa/moradia/itens/PDM/trabalhos e arquivos de configuração anteriores permanecem.

Comparados ApplyHeadBlend, MorphNames, ApplyMorph e ApplyFaceMorphs em MultiplayerCharacter da PSRP.dll, com assinatura dos natives e enums do SDK instalado. Código próprio. A referência cria freemode masculino, aplica durante a seleção e limita morphs a 0..1; RPCore exige freemode existente, consulta origens disponíveis, permite formato/pele independentes e os dois sentidos nativos -1..1, aplicando apenas depois de confirmar. Não se declara paridade completa com o criador PSRP.

Criar/trocar ped, identidade completa, terceiro contribuinte, tatuagens, câmera dedicada e prévia ao vivo reversível seguem pendentes. Refinamento dos menus/mecânicas na fase 8 e de todos os trabalhos na fase 9 permanece previsto. GTA não executado nesta nuvem; sem leitura completa dos morphs/resultado de renderização, testes automáticos não comprovam o rosto no jogo.

## Testes dentro do GTA

1. Com freemode na barbearia, selecione herança e alguns traços; cancele. Repita Esc, F2/F11, afastamento e troca de perfil. Nada deve alterar aparência/banco antes de confirmar. Protagonista deve ser recusado sem mudança de ped.
2. Selecione somente um traço, com herança e demais grupos desativados. Confirme negativo, zero e positivo em atendimentos diferentes; confira alteração do índice e preservação dos outros grupos. Teste os 20 índices, principalmente lados do nariz, mandíbula e queixo.
3. Ative apenas herança. Varie as quatro origens, teste misturas 0/50/100 e confirme. Aguarde o aviso de conclusão antes de abrir outra interface. Deve cobrar uma taxa só e mudar a base do rosto/pele, sem trocar ped/roupa/perfil.
4. Combine herança, dois traços e maquiagem/barba/cabelo: uma confirmação, taxa única. Confira aplicação dos detalhes depois do rosto carregado. Depois mude só um traço: demais registros devem permanecer. Reaplicar confirmado deve recuperar todos os grupos registrados sem cobrar.
5. Sem grupo, recusa cobrança. Com saldo insuficiente, nenhuma aplicação. Mesmos valores registrados devem indicar opção gratuita. Reinicie: aparência não é aplicada nem cobrada automaticamente.
6. Se a aplicação for interrompida/expirar ou falhar, confira pendência e retome no mesmo modelo. Depois da autorização não deve oferecer estorno. Registro antigo de maquiagem e pedido antigo de barba/tintura devem continuar utilizáveis sem novos grupos automáticos.
7. Repita cortes/olhos, roupas, moradia/storage/inventário, F2/F11/J/teleporte, PDM e trabalhos. Verifique aparência com seus recursos instalados e informe qualquer recomposição inesperada após head blend.

Build Release sem erros/avisos. 1332 testes passaram (65 novos). Relatórios DataTests-0.37.0.trx, PSRP-SYSTEMS-AUDIT-0.37.0.json e VALIDACAO.md distinguem testes de dados/instalador do teste real de GTA.
