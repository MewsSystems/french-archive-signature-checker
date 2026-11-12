# French archive signature checker

[![build-and-test](https://github.com/MewsSystems/french-archive-signature-checker/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/MewsSystems/french-archive-signature-checker/actions/workflows/build-and-test.yml)
[![CodeQL](https://github.com/MewsSystems/french-archive-signature-checker/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/MewsSystems/french-archive-signature-checker/actions/workflows/codeql-analysis.yml)

`Mews.Fiscalization.SignatureChecker` checks that a French fiscal archive (NF525) exported from Mews has not been changed since Mews signed it.

It reads the archive ZIP file, rebuilds the data that Mews signed (tax totals per rate, the grand total, the archive's creation time, terminal, type, the hash of the archive files for version 4.1.1, and the previous archive's signature), then verifies the signature in `SIGNATURE.txt` against the Mews public key. If any figure that is part of the signature changed, verification fails.

## Using the tool

The tool is distributed as a ZIP file with a `WindowsOS` and a `MacOS` folder. Each folder has a standalone executable, so you don't need to install .NET.

### Windows

1. Extract the ZIP file.
2. Open the `WindowsOS` folder and run `Mews.Fiscalization.SignatureChecker.exe`.

### macOS

1. Extract the ZIP file.
2. Open Terminal and go to the `MacOS` folder:
   ```sh
   cd path/to/MacOS
   ```
3. Remove the quarantine attribute that macOS adds to downloaded files:
   ```sh
   xattr -dr com.apple.quarantine Mews.Fiscalization.SignatureChecker
   ```
4. If you get a permission error, make the file executable:
   ```sh
   chmod +x Mews.Fiscalization.SignatureChecker
   ```
5. Run the tool:
   ```sh
   ./Mews.Fiscalization.SignatureChecker
   ```

### Verifying an archive

You can use the tool interactively, or pass the archive on the command line to check it once.

#### Interactive

Start the tool without arguments. It asks:

```
Enter the archive zip file path and options (e.g. 2025.zip --develop or 2025.zip --production), or drag the file into this window:
```

Type the path to the archive ZIP file, or drag the file into the window, and press Enter. Paths with spaces work if they're in quotes (`"C:\My Archives\2025.zip"`) or, on macOS, with escaped spaces (`My\ Archives/2025.zip`), which is what you get when you drag a file into Terminal.

After each archive the tool asks `Do you want to verify another file? (yes/no):`. Type `yes` to check another archive; anything else exits.

#### Command line

Pass the path and an option to check one archive and exit:

```sh
Mews.Fiscalization.SignatureChecker 2025.zip
Mews.Fiscalization.SignatureChecker "C:\My Archives\2025.zip" --develop
```

The exit code tells you the result, so you can use the tool in scripts:

| Exit code | Meaning |
| --- | --- |
| `0` | The archive signature is valid. |
| `1` | The archive signature is not valid, or the archive couldn't be read. |
| `2` | The arguments are invalid, or the file doesn't exist or isn't a ZIP file. |

#### Options

| Option | Meaning |
| --- | --- |
| `<path-to-archive>` | Required. Path to the archive ZIP file, absolute or relative to the folder you started the tool from. Exactly one path is expected. |
| `--production` | Verify with the Mews **production** key. This is the default. |
| `--develop` | Verify with the Mews **develop** key, for archives created on a Mews test environment. |
| `--help` | Show the usage. |
| `--version` | Show the tool version. |

Options aren't case-sensitive. Unknown options (such as a mistyped `--develp`) are rejected, and you can't give both `--develop` and `--production`.

### Results

Before the result, the tool shows which key it used and what the archive says about itself, so you can check you're looking at the right archive:

```
Checking 2025.zip with the Mews production key.
Archive version 4.1.1, type FISCALYEAR, created 2026-01-01 03:00:12, terminal TERMINAL-1, chained to previous archive: yes.
Archive signature is valid.
```

| Output | Meaning |
| --- | --- |
| `Archive signature is valid.` (green) | The archive is unchanged since Mews signed it. |
| `Archive signature is not valid. The archive contents do not match its signature for the Mews production key.` (red) | The data was changed, the archive was signed by another environment (the tool suggests trying `--develop` or `--production`), or it isn't a Mews archive. |
| `Archive signature is not valid. <reason>` (red) | The archive is incomplete or malformed, for example `No unique file found METADATA.json*.`, `Archive version 3.0 is not supported.`, `Currency not found.` or `Invalid data (INVOICE_FOOTER-2025.csv, line 12): expected at least 19 columns, found 3.`. |
| `File <path> does not exist.` / `Cannot read archive: ...` (red) | The path is wrong or the file isn't a ZIP file. In interactive mode the tool asks for a path again. |

## Supported archives

| Archive version (`METADATA.json`) | Archive types | Signed data | Signature algorithm |
| --- | --- | --- | --- |
| `1.0` | archiving | `TAX_TOTALS*` and `TOTALS*` files | RSA PKCS#1 v1.5, SHA-1 |
| `4.0`, `4.1` | `DAY`, `MONTH`, `FISCALYEAR` | `INVOICE_FOOTER*` files | RSA PKCS#1 v1.5, SHA-256 |
| `4.1.1` | `DAY`, `MONTH`, `FISCALYEAR` | `INVOICE_FOOTER*` files, plus a SHA-256 hash of every `.csv` and `.html` file in the archive | RSA PKCS#1 v1.5, SHA-256 |

Every archive must contain exactly one `METADATA.json` and one `SIGNATURE.txt`. Amounts in version 4 archives must be in euros (`€` or `EUR`).

## Development

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). The SDK version is pinned in [global.json](global.json).

```sh
dotnet build Mews.Fiscalization.SignatureChecker.slnx
dotnet test Mews.Fiscalization.SignatureChecker.slnx
```

Run the tool from source:

```sh
dotnet run --project src/Mews.Fiscalization.SignatureChecker
```

### Project layout

| Path | Contents |
| --- | --- |
| `src/Mews.Fiscalization.SignatureChecker` | The console tool. `ConsoleApplication` runs the interactive and command-line modes; `CommandLine` parses paths and options; `SignatureVerifier` rebuilds the signed payload and checks the signature; `Model` parses the archive, and `ArchiveStructure` checks required files and columns first; `Dto` holds the raw ZIP and CSV contents; `CryptoServiceProvider` embeds the Mews public keys. |
| `tests/Mews.Fiscalization.SignatureChecker.Tests` | xUnit tests. They sign archives with a throwaway key so the full verification path runs, pin the expected signed payload for every archive version, pin the fingerprints of the embedded Mews public keys, and run the built executable end to end. |

### Building the distributable executables

The standalone executables in the distribution ZIP are self-contained, single-file builds:

```sh
dotnet publish src/Mews.Fiscalization.SignatureChecker -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/WindowsOS
dotnet publish src/Mews.Fiscalization.SignatureChecker -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true -o publish/MacOS
```

Use `osx-x64` instead of `osx-arm64` for Intel Macs.

### Changing the tool

This tool is used in fiscal audits, so a change must never alter which archives it accepts or rejects. Before you change parsing or signature logic:

- Keep the tests in `SignatureVerifierTests` passing unchanged. They pin the exact signed payload for each archive version.
- Don't change the public keys in `CryptoServiceProvider.cs`. `CryptoServiceProviderTests` pins their fingerprints.
- Upgrade dependencies that affect parsing (`FuncSharp`, `Newtonsoft.Json`) deliberately, and check the results against real archives.

CI builds and tests on Windows, Linux and macOS for every pull request, and CodeQL scans the code weekly.
