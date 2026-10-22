# RPCore 0.5.1 — downloads

- [Atualização 0.5.1 — DLL pronta e instalador](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.5.1.zip)
- [Código-fonte 0.5.1 — projeto para compilar](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.5.1.zip)
- [Auditoria da PSRP.dll / mapa dos dicionários e imagens](../docs/AUDITORIA-YTD-0.5.1.md)

A DLL usa as imagens já instaladas em scaleform_generic.rpf. Metadados padrão embutidos corrigem a dependência de JSONs auxiliares ausentes; overrides locais ficam preservados. F11 passa a alternar os três banners como a referência. F10 > Visual assets: diagnose gera VisualDiagnostics.json com consultas reais ao motor do GTA.

Feche o GTA e execute Install-RPCore.ps1 do pacote update. Preserva saves/configurações e guarda backup. Não precisa reinstalar o OIV já instalado. Build sem erros/avisos, 112 testes. O diagnóstico e renderização no jogo ainda exigem execução no computador do usuário.

Controles e composição da 0.5.0 permanecem: I grade, F1 celular, J radial, F11 configurações, F2 garagens, F10 banco/perfis/diagnóstico. Limites de funcionalidades: INTERFACE-0.5.0.md. O csproj fica no pacote source. Nomes sem versão apontam para a entrega atual.

[Retornar à 0.5.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.5.0.zip). Os [recursos visuais originais 0.3.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-visual-assets-0.3.0.zip) continuam os mesmos, para instalações que ainda não os possuem.
