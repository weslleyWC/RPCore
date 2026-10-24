# RPCore 0.36.0 — fase 6.3C: pele e maquiagem

## Instalação e acesso

Baixe e extraia RPCore-update-0.36.0.zip da branch rpcore-entrega. Abra PowerShell na pasta extraída que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Requer GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não precisa reinstalar .oiv/.rpf. PSRP.dll é referência de análise, não outro script para executar junto. Instalador preserva saves/configurações, inclusive Price personalizado em Jobs/Grooming.json, F2 celular e F11/teleporte.

Perto de uma das sete barbearias, a pé e sem atividade: **F10 → Barbearias / cortes e olhos → Acabamento / pele e maquiagem → Configurar novos valores**. Exige freemode masculino/feminino já carregado, com head blend pronto. Não troca protagonista nem cria ped automaticamente. Cortes/olhos continuam no menu anterior.

## Implementação

O atendimento existente de tintura/barba/sobrancelhas foi ampliado com 11 grupos independentes:

| ID native | Grupo | Paleta |
| --- | --- | --- |
| 0 | Marcas da pele | Sem tintura |
| 3 | Envelhecimento | Sem tintura |
| 4 | Maquiagem | Maquiagem |
| 5 | Blush | Maquiagem |
| 6 | Aspecto da pele | Sem tintura |
| 7 | Danos solares | Sem tintura |
| 8 | Batom | Maquiagem |
| 9 | Pintas/sardas | Sem tintura |
| 10 | Pelos do peito | Pelos |
| 11 | Marcas do corpo | Sem tintura |
| 12 | Marcas adicionais do corpo | Sem tintura |

Abra o grupo e marque **Alterar este grupo: SIM** para editar estilo, opacidade em passos de 10% e cor quando disponível. As opções anterior/próximo incluem **sem**, que remove somente aquele overlay após confirmar. Opacidade zero deixa o grupo invisível. Voltar ao atendimento mantém a seleção; desmarcar o grupo exclui apenas sua escolha do rascunho, sem remover o visual atual. Fechar ou Cancelar selecao descarta tudo sem aplicação/cobrança.

Todos os grupos começam desativados, mesmo que exista um registro anterior. Ao ativar, usa os valores já registrados para esse perfil/modelo; se não houver, lê apenas o índice de estilo atual e oferece NOVA opacidade de 100% e NOVA cor 0. A API Legacy não fornece leitura completa de cores/opacidades atuais. Não há prévia aplicada no personagem: a amostra RGB mostra a paleta, com getter de maquiagem para maquiagem/blush/batom e de pelos para peito. Alguns estilos de maquiagem têm cores próprias e podem responder parcialmente à paleta; teste visual necessário.

Volte ao atendimento e **Confirmar aplicacao**. Taxa única pelo banco RPCore, padrão **$120 pelo conjunto**, não por grupo. Jobs/Grooming.json existente define Price; não há configuração nova nem taxa extra sobre tintura/barba. Cortes/olhos permanecem outro serviço de $150. Nenhum grupo selecionado: recusa cobrança. Grupos já registrados com os mesmos valores: aplicação gratuita. Alterar só batom não reaplica cabelo, barba, pele ou qualquer grupo não escolhido.

Pedido, autorização e conclusão usam o ciclo persistente da 0.35: salva taxa/pedido antes dos setters, marca início antes da aplicação e conclui mesclando só grupos escolhidos. Interrupção mantém pendência para retomar sem novo débito no mesmo modelo. Antes do início, pode cancelar/estornar; depois do início, pode retomar ou encerrar explicitamente sem estorno nem restauração inventada das cores antigas. Valores confirmados podem ser reaplicados manualmente na loja, sem cobrar. Nunca aplica automaticamente no load/perfil.

## Compatibilidade e referência

Adicionado Overlays em Wallet.Grooming.Looks e Pending.Look, com valores opcionais vazios para registros antigos. Não migra nem apaga looks/pendências de tintura, barba ou sobrancelhas. Merge conserva grupos não selecionados, campos desconhecidos e modelo/perfil. Limites de estilo/paleta consultados no GTA antes da aplicação; estrutura valida IDs, duplicatas, valores e seleção vazia. Não altera modelo, herança/morfologia facial, cortes, olhos, roupas, tatuagens, dinheiro de outros sistemas, arquivos .oiv/.rpf ou os controles existentes.

Comparados ApplyBlemishes, ApplyAgeing, ApplyComplexion, ApplySunDamage, ApplyMolesFreckles, ApplyMakeup e ApplyBlush de MultiplayerCharacter na PSRP.dll, além de documentação dos natives e os enums do SDK instalado. RPCore usa código próprio e confirmação recuperável. Batom, pelos do peito e marcas corporais ampliam os controles nativos; não se afirma equivalência desses extras com um fluxo completo da PSRP. Confira PSRP-SYSTEMS-AUDIT-0.36.0.json.

Não é criador completo: herança facial, morphs, criação/troca de ped, tatuagens e câmera dedicada continuam pendentes. As fases 8 (interface/mecânicas) e 9 (todos os trabalhos) permanecem previstas. Roupa pode esconder overlays corporais. A aparência final depende dos recursos do ped/mods e precisa do GTA; não executado nesta nuvem.

## Testes no GTA

1. No freemode e perto da barbearia, configure Maquiagem, Blush e Batom separadamente. Confira navegação, amostras, estilo, opacidade e aplicação. Cada confirmação nova cobra uma taxa; combinar três grupos cobra uma só. Outros grupos atuais devem permanecer.
2. Escolha pele/envelhecimento/pintas e cancele; repita Esc, F2/F11, afastar-se e trocar perfil. Nada deve alterar visual/banco durante seleção. Nenhum grupo marcado deve recusar pagamento.
3. Teste cada um dos 11 grupos, opção sem e opacidade zero. Para peito/corpo use roupas que deixem a região visível. Sem deve remover apenas o grupo escolhido; desmarcar NÃO deve remover nada.
4. Confirme só um grupo, depois outro: reaplicação gratuita deve conservar os dois registros. Repetir os valores registrados não deve cobrar. Reiniciar não aplica nem cobra automaticamente.
5. Confira tintura/mechas, barba/sobrancelhas e cortes/olhos antigos, inclusive looks da 0.35. Pendência existente continua retomável pelo mesmo preço salvo, sem selecionar grupos novos. Saldo insuficiente não deve aplicar nada.
6. Protagonista deve ser recusado para novos overlays sem mudar modelo. Modelos/perfis diferentes devem ficar isolados. Confira F2/F11/J, inventário/storage/roupas/moradia, PDM e trabalhos.

Build Release: zero erros/avisos. 1267 testes de dados/transações passaram (34 novos), incluindo todos os IDs/paletas, limites, merge/metadata, snapshot/reload, falhas e pendência antiga. Relatórios e validação do instalador em VALIDACAO.md. Os testes não simulam renderização ou natives GTA.
