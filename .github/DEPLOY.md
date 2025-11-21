CI/CD via FTP

This repository includes a GitHub Actions workflow at `.github/workflows/ci-cd.yml` that:

- Builds the solution (`dotnet build`)
- Runs tests (`dotnet test`)
- Publishes the API (`dotnet publish`)
- Deploys the published output to your FTP server

Required GitHub Secrets

Add the following repository secrets (Settings → Secrets & variables → Actions):

- `FTP_HOST` - your FTP server address (e.g. `guia-noivas.somee.com` or the IP from your provider)
- `FTP_USERNAME` - FTP username (the attachment shows `bortojh`)
- `FTP_PASSWORD` - FTP password (please add the value — do NOT commit it)
- `FTP_REMOTE_PATH` - remote target folder on the FTP server (e.g. `/www.guia-noivas.somee.com`).

Note: the workflow expects the password secret to be named `ftp_password` (lowercase) in the Actions secrets. To support both naming conventions, you can set both `FTP_PASSWORD` and `ftp_password` to the same value — the workflow uses `ftp_password`.

Notes & usage

- The workflow triggers on push to `main` and can also be started manually from the Actions tab.
- The workflow publishes the project to `src/GuiaNoivas.Api/bin/Release/net9.0/publish` and uploads that folder contents.
- If your server expects a different remote path (or subfolder), set `FTP_REMOTE_PATH` accordingly.
- If you want deployments from a different branch (for example `dev-hb`), edit `.github/workflows/ci-cd.yml` triggers.

Troubleshooting

- If upload fails with permission errors, verify `FTP_USERNAME`/`FTP_PASSWORD` and the `FTP_REMOTE_PATH` value.
- If IIS needs a recycle after files are uploaded, you can either configure your host panel to auto-recycle or extend the workflow with an SSH or control-plane API (if available).

Security

- Keep FTP credentials in GitHub Secrets only. Do not add them to the repo.

If you want, I can:
- Change the workflow to deploy on push to `dev-hb` instead of `main`.
- Use a different FTP action or an `lftp` script if your server requires advanced mirroring options.
- Add an optional step to create a backup of the remote folder before deploying.
