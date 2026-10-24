# RPCore 0.35.0 — fase 6.3B: tintura, barba e sobrancelhas

## Instalação e acesso

Extraia RPCore-update-0.35.0.zip e abra PowerShell na pasta com Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Ajuste o caminho. Requer GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não execute PSRP.dll como segundo script. Sem reinstalar .oiv/.rpf. Conserva saves/configs e sistemas anteriores; adiciona Jobs/Grooming.json. F2/F11/F8 mantidos.

Perto de uma das sete barbearias: F10 → Barbearias / cortes e olhos → Tintura / barba / sobrancelhas. Novos grupos exigem ped freemode masculino/feminino com rosto já carregado. Protagonistas continuam com os cortes disponíveis da fase anterior, mas não recebem estes overlays. Esta subfase não troca/cria ped. A tecla E nativa continua independente.

## Seleção e confirmação

Configurar novos valores inicia um rascunho com todos os grupos em NAO. Ative apenas o que quer substituir:

- Tintura: nova cor do cabelo e mechas, índices de paleta 0–63.
- Barba: estilo, opção sem barba, opacidade 0–100% e nova cor.
- Sobrancelhas: estilo, opção sem sobrancelhas, opacidade e nova cor.

Cada grupo marcado SIM substitui TODOS os seus valores exibidos ao confirmar. Valores exibidos são os novos escolhidos ou os que você já registrou, não uma leitura completa das cores atuais. Grupos em NAO não são aplicados. A amostra RGB aparece nas linhas de cor; representa a paleta, sem pintar o personagem. Não há prévia ao vivo de barba/tintura/opacidade nesta subfase. Cancelar, fechar, abrir outra interface, afastar ou trocar perfil descarta o rascunho sem alterar aparência ou dinheiro.

Confirmar aplicacao cobra **$120 do banco RPCore por atendimento**, independentemente de quantos grupos estejam selecionados. É separado do serviço de cortes/olhos de $150. A taxa é pela aplicação escolhida; cores não registradas antes podem coincidir com o visual atual, pois não há leitura completa para detectar isso. Seleção sem nenhum grupo não cobra. Valores selecionados que já constem no registro do modelo são reaplicados gratuitamente e o botão indica isso.

Só depois de confirmar/gravar o pedido os setters modificam o personagem. Conclusão salva os grupos escolhidos e mantém os outros grupos registrados. Não mexe em rosto genético, modelo, roupa, corte, olhos ou tatuagens. Nada é aplicado automaticamente ao carregar scripts ou trocar perfil.

## Pendências e reaplicação

Antes de iniciar os setters, o mod salva o estado de autorização do pedido. Assim, se a operação for interrompida, Retomar atendimento pendente reaplica os mesmos valores no mesmo modelo e conclui sem cobrar outra vez. Reaplicar acabamento confirmado também é gratuito, na loja, e aplica os grupos que você já confirmou para aquele perfil/modelo.

Pedido ainda não iniciado oferece Cancelar pendente e estornar: devolve taxa e contadores. Se banco estiver no limite inteiro, pendência fica guardada até liberar espaço, sem descartar dinheiro. Depois de iniciado, parte ou todo o visual pode já ter sido alterado e as cores originais não são recuperáveis pelo getter Legacy. Nessa situação há retomada gratuita ou Encerrar pendente sem estorno, com confirmação explícita. Encerrar não restaura cores anteriores nem devolve a taxa; use retomar para concluir o serviço pago. Não há estorno que prometa desfazer cores desconhecidas.

## Roteiro no GTA

1. Com freemode e rosto carregado, vá à loja e abra o novo menu. Ative/mude cores, estilos/opacidade e depois cancele: nada no personagem/banco deve mudar. Repita fechar, F2/F11 e afastar-se durante rascunho.
2. Marque somente Tintura, escolha cor e mechas diferentes, confira amostra, confirme. Deve cobrar $120 uma vez e pintar somente cabelo/mechas, sem trocar corte, barba, sobrancelhas ou roupa. Reinicie: não cobra/aplica automaticamente; use reaplicação manual para restaurar os valores confirmados.
3. Marque somente Barba, escolha estilo/opacidade/cor e confirme. Cabelo e sobrancelhas atuais devem permanecer. Faça o mesmo só com Sobrancelhas. Confira opção sem (remoção apenas do overlay selecionado) e opacidade zero. Resultados dependem de recursos do modelo; teste no GTA.
4. Selecione dois/três grupos juntos: um atendimento, taxa única $120. Depois altere só um grupo: registro dos outros continua, mas esses grupos não devem ser reaplicados durante essa nova confirmação. Reaplicar acabamento confirmado aplica todos os grupos registrados, explicitamente.
5. Sem grupo, deve recusar cobrança. Valores já registrados devem mostrar opção gratuita. Saldo insuficiente deve conservar banco e personagem. Opcionalmente use Price=0 em Grooming.json, com jogo fechado, para testar aplicação sem custo.
6. Caso ocorra falha/interrupção depois da confirmação, confira pedido pendente e Retomar no mesmo freemode, sem nova cobrança. Estorno só aparece antes da autorização; depois, encerramento sem estorno pede confirmação. Não precisa causar falha real no arquivo: rollback/replay foram testados automaticamente.
7. Troque perfil/modelo: não deve aplicar visual do anterior no novo. Protagonista deve ser recusado para novos overlays, sem troca automática de modelo. Acesso aos estornos/pendências não deve aplicar aparência sozinho.
8. Repita cortes/olhos anteriores, guardarroupa (que não muda cabelo), moradia, storage, F8/trunk, inventário, radial J, F2/F11/teleporte, trabalhos e PDM. Estilos/pagamentos antigos ficam preservados.

## Configuração, referência e limites

Jobs/Grooming.json define Price de 0 a 10000 (padrão 120). Não reescreve Barber.json. Pedido pendente conserva preço/valores escolhidos, mesmo após trocar configuração. Cores/estilos são validados contra recursos disponíveis antes de autorizar aplicação. Head blend precisa estar pronto; o mod não cria um rosto padrão para habilitar overlays.

Analisados ApplyHair, ApplyFacialHair e ApplyBrows na PSRP.dll. Natives de tintura/overlays e grupos 1/2 seguem a referência; ciclo de seleção/compra/pendência é próprio. GET_PED_HAIR_TINT_COLOR consulta RGB de paleta e GET_PED_HEAD_OVERLAY apenas índice: não são usados como backup completo de cor/opacidade. Sem offsets de memória, prévia impossível de reverter ou zerar traços não escolhidos.

Não é criador completo de personagem. Herança facial, maquiagem, tatuagens, criação de ped, câmera de atendimento e demais diferenças continuam em subfases/refinamentos. Fases 8/9 mantidas. Veja VALIDACAO.md e PSRP-SYSTEMS-AUDIT-0.35.0.json. GTA não executado nesta nuvem.
