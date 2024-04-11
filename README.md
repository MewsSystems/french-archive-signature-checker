# French archive signature checker

## Mews.Fiscalization.SignatureChecker

This is a tool that can be used to verify the signature of the French fiscal archives. It loads the data that are part of the signature from the ZIP archive and verifies the attached signature using the Mews public key.

### Usage

```Mews.Fiscalization.SignatureChecker.exe --<path-to-archive> --<environment>```

### Parameters

- `path-to-archive` - Path to the ZIP archive that contains the signature and the data that are part of the signature.
- `environment` - Environment for which the signature should be verified. Possible values are `production` and `develop`, if not provided, `production` environment will be used.

### Output

The tool will output the result of the signature verification. If the signature is valid, the tool will output `Archive signature is valid.`. If the signature is invalid, the tool will output `Archive signature is invalid.`.

### Example

```Mews.Fiscalization.SignatureChecker.exe --C:\path\to\archive.zip --develop```