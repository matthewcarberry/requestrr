# Running this fork on Unraid

The image is built by `.github/workflows/docker-publish.yml` on every push to
`master` and published to `ghcr.io/matthewcarberry/requestrr` (tags: `latest`,
the short commit SHA, and the version for `v*` tags).

## Swapping an existing Requestrr container

The fork reads the same settings as upstream Requestrr, so it can take over
your existing appdata folder.

1. Back up the existing config folder (usually `/mnt/user/appdata/requestrr`).
2. Docker tab: click the Requestrr icon, then **Edit**.
3. Change **Repository** to `ghcr.io/matthewcarberry/requestrr:latest`.
4. Check the config path's **Container Path** is `/root/config`. Some
   Requestrr templates map `/config`, which this image does not use, so
   settings would be lost on every update.
5. Click **Apply**. Unraid pulls the new image and recreates the container.

To go back, set Repository to `thomst08/requestrr:latest` and apply again.

## Testing alongside an existing Requestrr

Run the fork as a second container with its own config folder and port:

```sh
docker run -d --name=requestrr-books --restart=unless-stopped \
  -p 4546:4545 -v /mnt/user/appdata/requestrr-books:/root/config \
  ghcr.io/matthewcarberry/requestrr:latest
```

If you clone your existing template instead, set the config **Container
Path** to `/root/config`.

Give it its own Discord bot (a new application in the Discord developer
portal). Two containers logged in with the same bot token fight over the
slash commands. When you are happy with it, swap the main container over as
described above and remove this one.

## Fresh install

Copy `requestrr.xml` to `/boot/config/plugins/dockerMan/templates-user/my-requestrr.xml`
on the Unraid flash drive, then in the Docker tab choose **Add Container** and
pick `requestrr` from the template list. Or from the Unraid terminal:

```sh
docker run -d --name=requestrr --restart=unless-stopped \
  -p 4545:4545 -v /mnt/user/appdata/requestrr:/root/config \
  ghcr.io/matthewcarberry/requestrr:latest
```

Then open `http://<unraid-ip>:4545` and set up Bookshelf under Books and
Listenarr under Audiobooks.

## Package visibility

GHCR packages start private. Either make the package public
(GitHub > Packages > requestrr > Package settings > Change visibility), or log
Unraid in once with a personal access token that has `read:packages`:

```sh
docker login ghcr.io -u matthewcarberry
```
