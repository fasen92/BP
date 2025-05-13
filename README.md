# Lokalizácia Wi-Fi prístupových bodov
**Autor:** Ivan Mahút (xmahut01@stud.fit.vutbr.cz)

**Vedúci práce:** Ing. Jan Pluskal, Ph.D.
## Abstrakt
Táto práca sa zaoberá lokalizáciou Wi-Fi prístupových bodov spracovaním lokalizačných
údajov zozbieraných z bezdrôtových sietí. V úvodnej časti je preskúmaná problematika lokalizácie prístupových bodov a analyzované metódy a techniky používané na určovanie ich
polohy. Navrhnutá metodika lokalizácie vychádza zo zberu geografickej polohy v kombinácií s meraním intenzity prijatého signálu. Tieto údaje sú spracovávané webovou aplikáciou,
ktorá pomocou nelineárnej metódy najmenších štvorcov a metódy váženého ťažiska aproximuje polohy prístupových bodov. Výsledné polohy prístupových bodov sú zobrazované
na interaktívnej mape ako jednotlivé body alebo formou tepelných máp. Presnosť lokalizácie bola overená testovaním so známymi polohami prístupových bodov. Aplikácia je
implementovaná s použitím ASP.NET Core a PostgreSQL.
## Kompilácia a spustenie
Pre spustenie aplikácie je potrebné mať nainštalované:
- **Docker Desktop** vo verzií `28.0.4+` 
- **Docker Compose** vo verzií `v2.34+`
Následne stačí v koreňovom adresáry zdrojových kódov `src/`, v ktorom sa nachádza *docker-compose.yml* zadať nasledujúce príkazy:
 ```bash
docker compose build
docker compose up
```
Frontend je následne dostupný na adrese http://localhost:7062/. Databáza už obsahuje nazbierané lokalizačné záznamy a vypočítané polohy prístupových bodov na fakulte FIT VUT. Do aplikácie môžu byť nahraté výstupné súbory aplikácie Wigle Wifi Wardriving. 
