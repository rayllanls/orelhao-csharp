# Orelhão — guia do projeto

Documento de contexto para quem (pessoa ou IA) for continuar o projeto.
Resume o que o app é, como é construído, o que já foi feito e o que falta.

> **Regra de ouro:** nada de dado real da rede em código, testes, exemplos ou
> docs. Sem IPs internos, matrículas, hostnames corporativos, jump hosts. Eles
> ficam só no `shortcuts.json` local, que **não** é versionado. Antes de cada
> commit, varra o que vai subir por esses padrões.

## O que é

Gerenciador de atalhos de terminal para Windows, em **C# / .NET 8 (WPF)**.
Guarda comandos (SSH e quaisquer outros) em grupos e abre cada um numa janela
nova. Nasceu para encurtar sessões SSH via proxy (formato
`ssh id@conta@servidor@proxyjump`), mas é **agnóstico** — serve para qualquer
comando. Sem marca de fornecedor na interface.

> O app começou como um script Python (CustomTkinter) e foi **reescrito em C#**
> em 2026-07-18 para virar um app Windows nativo. Os arquivos Python foram
> removidos; o histórico da migração está ao final.

## Como rodar

```bash
dotnet run --project src/Orelhao/Orelhao.csproj   # rodar em dev
dotnet test                                        # 29 testes (xUnit) sobre o Core
dotnet publish src/Orelhao/Orelhao.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -o publish   # gera publish/Orelhao.exe (~70 MB)
```

Requer o **.NET 8 SDK**. NuGets: `WPF-UI`, `DnsClient`, `CommunityToolkit.Mvvm`.

## Arquitetura

Solução `Orelhao.sln` com três projetos, separando lógica pura da GUI:

- **`src/Orelhao.Core`** (sem UI, testável): `SshComposer` (build/parse do
  `ssh a@b@c@d`), `DataStore` (normalização tolerante via `JsonNode` + escrita
  atômica `.tmp`→`File.Move`), `DnsService` (DnsClient.NET — resolução por tipo
  A/AAAA/CNAME/MX/TXT/NS/PTR + reverse/forward + **propagação**: consulta cada
  servidor em paralelo e compara), `IpLookup` (HttpClient→ipinfo),
  `WebInspector` (cabeçalhos HTTP + certificado SSL/TLS via SslStream/X509, sem
  deps novas), `NetworkInfo` (IP local, privado/público), modelos e `Constants`.
  É o que `Orelhao.Tests` cobre (as consultas de rede foram validadas com testes
  de fumaça, removidos por dependerem de internet).
- **`src/Orelhao`** (app WPF): `MainWindow` é um `ui:FluentWindow` com uma
  **barra lateral de navegação própria** (RadioButtons estilizados + `Frame`),
  não o `NavigationView` do WPF-UI (que ignorava `OpenPaneLength`). As 7
  páginas são `Page` em `Views/`; cada troca de aba cria a página nova (relê o
  estado). `ShortcutStore` (em `Services/`) é a **fonte única de estado** —
  coleções observáveis de grupos/atalhos/DNS/histórico + ordem de favoritos +
  tema — compartilhada por todas as páginas via `App.Store`. ViewModels
  observáveis (`GroupViewModel`, `ShortcutViewModel`) usam CommunityToolkit.Mvvm.
  `ThemeService` aplica os temas; `IconCatalog` lista os ícones.
- **`tests/Orelhao.Tests`**: xUnit sobre o Core (29 testes).

- **Dados** em `shortcuts.json`, ao lado do executável (`AppPaths.DataDir` usa
  `Environment.ProcessPath`, que aponta para o .exe mesmo empacotado
  single-file). Escrita atômica; banco ilegível vira `.bak` e o app começa
  limpo (`DataStore.Load` devolve `LoadResult.Recovered`).
- **Handler global de exceção** em `App` (`DispatcherUnhandledException`): loga
  em `%TEMP%\orelhao_error.log`, avisa e **não derruba** o app.
- **Ganchos de teste** (env vars): `ORELHAO_PAGE=Home|Groups|Favorites|History|
  Ip|Settings|About` abre direto naquela aba; `ORELHAO_OPEN=shortcut|palette`
  abre o diálogo/paleta na inicialização (usados nos screenshots).

### Formato do `shortcuts.json`

```json
{
  "groups": [
    {"name": "SSH", "shortcuts": [
      {"name": "...", "command": "...", "icon": "Desktop24", "favorite": false}
    ]}
  ],
  "dns_servers": ["1.1.1.1", "8.8.8.8"],
  "ssh_defaults": {"id": "", "jump_proxy": ""},
  "history": [{"name": "...", "command": "...", "icon": "Desktop24", "when": "dd/mm/aaaa HH:MM"}],
  "favorite_order": ["<grupo><sep><nome>", "..."],
  "theme": "Dark",
  "accent_color": ""
}
```

`icon` é o **nome de um símbolo Fluent** (ex.: `Desktop24`) — o WPF não
renderiza emoji colorido, então os ícones são vetoriais (`IconCatalog`).
`DataStore.Normalize` completa campos ausentes e ignora lixo, então arquivos
antigos continuam abrindo (ícone desconhecido cai no padrão via `IconCatalog.Parse`).

## Convenções

- **Cor / temas:** `ThemeService.Apply(base, accentHex)`. A **base** é
  claro/escuro (padrão: escuro) e manda no **texto/títulos** (brancos no
  escuro, pretos no claro — nunca coloridos, para não poluir). A **cor de
  destaque** (vazia = amarelo `#FDBF2D`) pinta só os **ícones e elementos de
  destaque**: ícones do menu (`NavIcon` style) e dos atalhos (Foreground =
  `OrelhaoAccentBrush`), botões Primary, números, foco, Meu IP. Editada na
  janela `ThemeDialog` (base + paleta clicável + hex, preview ao vivo), aberta
  em Configurações; "Restaurar padrão" volta ao escuro + amarelo.
  Regra ao adicionar tela nova: **ícone** = `{DynamicResource OrelhaoAccentBrush}`;
  **texto** = padrão (herda a base).
- **Ícones:** vetoriais (`ui:SymbolIcon`), do WPF-UI. A grade de escolha é
  `IconCatalog.Choices` (`SymbolRegular[]`, compile-checked). O atalho guarda o
  nome do símbolo como texto; o `IconToSymbolConverter` converte na exibição.
- **Fonte:** os comandos em `Consolas` (monospace, `MonoFont`); o resto na
  fonte padrão do WPF-UI.
- Comentários em português, explicando o *porquê* das decisões não óbvias.

## Versionamento

- Repo privado: `github.com/rayllanls/orelhao`.
- Branch atual: `experimento-visual` (onde está toda a versão C#).
- `shortcuts.json`, `.env`, `bin/`, `obj/`, `publish/` e `.md` (menos README e
  este) ficam fora do versionamento.

## O que já foi feito

- App em WPF + WPF-UI (Fluent/Win11) com barra lateral própria e 8 páginas:
  **Início** (painel com contadores + favoritos lançáveis, **arrastáveis para
  reordenar**), **Grupos** (cards expansíveis, ações por linha: executar,
  favoritar, editar, duplicar, mover, excluir; busca), **Favoritos**,
  **Histórico** (limite 100, limpar), **DNS & IP** (abas: IP, Registros DNS por
  tipo, Propagação entre servidores), **Inspeção Web** (cabeçalhos HTTP +
  certificado SSL/TLS), **Configurações** (tema, DNS internos, padrões de SSH),
  **Sobre**.
- **Compositor SSH**: monta `ssh id@conta@servidor@proxyjump` de 4 campos; id e
  proxyjump viram padrão do próximo cadastro novo (nunca ao editar).
- **Paleta rápida** (Ctrl+K): lançador que busca e executa qualquer atalho.
- **Ícone vetorial por atalho** (grade `IconCatalog`); adotam a cor do tema.
- **Temas personalizáveis**: base clara/escura + cor de destaque livre (paleta
  clicável), modo fosforado, preview ao vivo, restaurar padrão.
- **Todos os diálogos** (Atalho, grupo, paleta) são `FluentWindow` com barra de
  título e X.
- Empacotado em `publish/Orelhao.exe` single-file self-contained (~70 MB).

## O que falta

1. Assinar o exe (SmartScreen avisa por não ser assinado).
2. Merge do `experimento-visual` no `main` e nova tag.

## Armadilhas (C#)

- **`ui:FluentWindow` + `WindowBackdropType` (Mica/Acrylic) exige
  `ExtendsContentIntoTitleBar="True"`** — senão crasha em `OnSourceInitialized`
  ("Cannot apply backdrop effect..."). Todos os diálogos usam Extends + uma
  `ui:TitleBar`.
- **WPF não renderiza emoji colorido** (COLR/CPAL) — sai monocromático/tofu.
  Por isso os ícones são vetoriais (`SymbolIcon`), não emoji.
- **Override de tema em runtime:** `ThemeService.Apply` troca brushes próprios
  (`OrelhaoAccentBrush`) e, no modo fosforado, os `TextFillColor*Brush` do
  WPF-UI via `Application.Current.Resources[...]`. Só pega onde o XAML usa
  **DynamicResource** — por isso o `ContentFrame` tem
  `Foreground="{DynamicResource TextFillColorPrimaryBrush}"` (ícones/textos das
  páginas herdam a cor). Os overrides são **removidos** antes de reaplicar
  (senão "grudam"). O fundo fica o escuro/claro padrão (não é esverdeado).
  Botões `Appearance="Primary"` pegam o acento via `ApplicationAccentColorManager`.
- **DPI a 200% (o monitor do autor):** para screenshots via `PrintWindow`, o
  processo que captura precisa ser DPI-aware (`SetProcessDPIAware`), senão pega
  só o quarto superior-esquerdo da janela ampliado.
- **UTF-8:** editar o `shortcuts.json` com `Get-Content`/`Set-Content` do
  **PowerShell 5.1** corrompe acentos/emoji — use `-Encoding UTF8` nas duas
  pontas (ou `[System.IO.File]::WriteAllText` com UTF8 sem BOM), ou edite pelo
  próprio app.

## Histórico da migração (2026-07-18)

O app era um script Python (CustomTkinter) já pronto e distribuído. O autor
pediu para **migrar para C#** (melhor para app Windows). Nesta sessão:

1. Solução `.NET 8` (Core / WPF / Tests). Instalado o SDK e configurada a fonte
   NuGet (faltavam).
2. **Lógica pura portada** para `Orelhao.Core`; os 23 testes viraram 29 em xUnit.
3. **UI reconstruída em WPF + WPF-UI**, com liberdade de repensar a UX.
4. Sidebar própria no lugar do `NavigationView`; seletor de tema; empacotamento.
5. Correções após teste do autor: crash dos diálogos (Mica sem Extends), handler
   global de exceção, copiar resultado do IP, paleta explicada.
6. **Ícones vetoriais** no lugar de emoji (WPF não faz emoji colorido); tema
   **Verde** (esverdeia texto/ícones/fundo); **arrastar-e-soltar** para
   reordenar favoritos no Início; **X em todos os diálogos**; remoção dos
   arquivos Python.

Feito com o Claude Code (Opus 4.8) em par com o autor.
