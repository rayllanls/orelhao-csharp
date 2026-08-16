# Orelhão

Gerenciador de atalhos de terminal para Windows. Guarda seus comandos SSH
(e quaisquer outros) organizados em grupos e abre cada um numa janela nova,
com um clique. Nasceu para encurtar o caminho até as sessões via CyberArk,
que exigem um comando longo e fácil de digitar errado.

<img src="icon.png" alt="" width="96" align="right">

## Como usar

O Orelhão é um programa único: **`Orelhao.exe`**. Não precisa instalar nada
(nem o .NET) — é só abrir.

1. Copie o `Orelhao.exe` para uma pasta sua (Documentos, Desktop, um pen
   drive, onde preferir).
2. Dê um duplo clique. Na primeira vez o Windows pode mostrar um aviso azul
   ("Windows protegeu seu computador") porque o programa não é assinado —
   clique em **Mais informações → Executar assim mesmo**.
3. Pronto. Crie um grupo, adicione seus atalhos e clique em **Executar**
   para conectar.

Seus atalhos ficam salvos num arquivo **`shortcuts.json`**, criado na mesma
pasta do `Orelhao.exe`. Para levar tudo para outro computador, basta copiar
os dois arquivos juntos.

## O que faz

- **Grupos e atalhos** numa lista em árvore, com busca por nome ou IP.
- **Compositor CyberArk**: em vez de digitar a linha inteira, você preenche
  quatro campos — ID, conta de destino, servidor e jump proxy — e ele monta
  o `ssh <id>@<conta>@<servidor>@<jump>`. O ID e o jump do último cadastro
  já vêm preenchidos no próximo.
- **Reordenar e duplicar** atalhos (botão direito, ou `Ctrl+Setas` e
  `Ctrl+D`) — útil para cadastrar vários servidores mudando só o endereço.
- **Paleta rápida** (`Ctrl+K`): digite parte do nome, Enter, conectou.
- **Consulta de IP**: IP privado resolve o nome no DNS interno; IP público
  consulta o ipinfo.io; hostname resolve para IP.
- Abre no Windows Terminal, caindo para PowerShell e depois `cmd` se não
  houver.

## Onde ficam os dados

Tudo em **`shortcuts.json`**, ao lado do programa: grupos, atalhos,
servidores DNS e os padrões do compositor SSH.

Na primeira execução, sem o arquivo, o app abre vazio e o cria ao salvar o
primeiro atalho. A gravação é atômica (escreve num `.tmp` e substitui),
então uma queda no meio não corrompe os dados; se o arquivo for encontrado
ilegível, ele é preservado como `.bak` e o app começa limpo em vez de
travar.

> Esse arquivo guarda endereços e contas reais da sua rede. Se for versioná-lo
> ou compartilhar a pasta, tenha isso em mente — ele **não** é enviado ao
> repositório deste projeto.

---

## Para desenvolvedores

O app é feito em **C# / .NET 8 (WPF)**.

### Stack

- **.NET 8 + WPF** — app desktop nativo do Windows.
- **[WPF-UI](https://github.com/lepoco/wpfui)** — controles e tema Fluent
  (estilo Windows 11), com claro/escuro e o acento amarelo da marca.
- **[DnsClient.NET](https://github.com/MichaCo/DnsClient.NET)** — a consulta
  de IP, resolvendo nome/PTR contra os DNS internos informados na tela (o
  `System.Net.Dns` não deixa escolher o servidor).
- **[CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)** —
  MVVM moderno (source generators) para os ViewModels observáveis.
- **`System.Net.Http` / `System.Text.Json`** — ipinfo.io e a persistência.
- **[xUnit](https://xunit.net/)** — os testes das funções puras.

### Estrutura

```
src/Orelhao.Core/   Lógica pura, sem UI (testável isolada):
                    SshComposer, DataStore (normalização + escrita atômica),
                    DnsService, IpLookup, NetworkInfo, modelos.
src/Orelhao/        App WPF: sidebar de navegação própria + Frame, as 7
                    páginas (Views), ViewModels e serviços (ShortcutStore,
                    ThemeService, AppPaths).
tests/Orelhao.Tests/ xUnit sobre o Core.
```

A separação isola a lógica pura (testável) da GUI. O
`ShortcutStore` é a fonte única de estado que todas as páginas compartilham.

### Rodando a partir do código

```bash
dotnet run --project src/Orelhao/Orelhao.csproj
```

### Testes

```bash
dotnet test
```

Cobrem a montagem/leitura do comando SSH e a normalização do banco (mais o
ida-e-volta da persistência) — as funções que decidem em que servidor você
entra e as que precisam aguentar um JSON estragado.

### Gerando o .exe

```bash
dotnet publish src/Orelhao/Orelhao.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Sai um `publish/Orelhao.exe` de arquivo único e **self-contained**: inclui o
runtime .NET, então roda em qualquer Windows sem instalar nada (por isso pesa
~70 MB). O `shortcuts.json` nasce ao lado do exe.

---

Feito por [Rayllan Leitão](https://rayllanleitao.com.br).
