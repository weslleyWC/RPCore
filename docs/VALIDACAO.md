# Validação RPCore 0.3.0 — 22/10/2026

| Verificação executada | Resultado |
|---|---|
| Setup no checkout cloud | Passou; SDK 8.0.419, referências oficiais Enhanced 1.1.0.6 / LemonUI 2.2 e restores por lockfiles |
| Build Release net48 | Passou; **0 erros, 0 avisos**, assembly 0.3.0.0 |
| Testes de dados/regressões | **63 passaram, 0 falharam, 0 ignorados**; inclui as 50 regressões anteriores e 13 casos de catálogo/vínculos/layout/navegação |
| Conteúdo visual recebido | OIV/ZIP originais preservados por SHA-256; oito YTDs, 679 texturas, 27 modelos e um YTYP |
| Inspeção RSC7/RPF7 | Dicionários e recursos de props descomprimidos para leitura, tamanhos de páginas verificados, RPF OPEN lido sem alterar o arquivo |
| Nomes de textura usados | Banners/itens/telefone/veículo e OutBar0–100 conferidos contra o catálogo extraído; lookup seguro para recursos ausentes |
| Inspeção de imagens | Previews de HUD, inventário, banners e telefone extraídos. Alpha do frame preto conferido; ordem de desenho ajustada para não cobrir wallpaper/apps |
| Helper de props | Duas execuções reais via PowerShell 7.6.7/Linux em pasta temporária: RPF exato de 127.979.008 bytes, update.rpf preservado; DLC diferente e pacote corrompido recusados |
| Atualização da DLL | Duas execuções sobre uma cópia do payload 0.2.0: **79 arquivos anteriores preservados**, save com combustível/garagem mantido byte a byte, novos arquivos visuais instalados e backup da DLL anterior |
| Pacotes finais | CRC/SHA-256 verificados; DLL do ZIP corresponde ao build; recursos visuais originais preservados em pacote separado |

SHA-256 da DLL: `6db18b62026b38725db57e3532de1da0e659b33256ef088d3fd536562a558377`.

Evidências: `DataTests.trx`, `visual-inspection.json`, `visual-installer-validation.json`, `installer-validation-0.3.0.json` e `visual-previews`. A validação anterior está em `VALIDACAO-0.2.0.md`.

Os testes executam os mesmos arquivos puros de persistência/economia/veículos/visuais utilizados pelo mod e usam os três saves PSRP reais como fixtures. Não são uma execução do GTA nem substituem natives por stubs para afirmar funcionamento visual.

**Não executados neste ambiente Linux:** instalação pelo OpenIV, edição do dlclist dentro do RPF, montagem do DLC no GTA, carregamento/desenho reais de texturas, animações/posição dos props, navegação real por controle e coexistência com os mods do usuário. Checklist: `VISUAIS-0.3.0.md`. O diagnóstico do jogo deve ser executado pelo usuário e registra disponibilidade de dicionários/modelos, não garante sozinho o resultado visual.

Cinco nomes de arquivos de modelo não tiveram hash localizado no YTYP durante a inspeção; isso não prova isoladamente validade/invalidez no GTA. Eles não foram ativados por novas ações. A composição visual é própria sobre a arte enviada, sem afirmar equivalência de todo o código/layout PSRP. Os demais sistemas amplos pendentes estão em ALTERACOES.md.

A primeira entrega teve confirmação de teste em GTA pelo usuário. Isso não valida automaticamente 0.2.0/0.3.0. As instruções de build cloud continuam no rascunho da configuração; nenhuma publicação cloud é afirmada nesta entrega.
