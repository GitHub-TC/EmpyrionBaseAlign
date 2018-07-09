# Empyrion Base Align
## FAQ

Eine übersetzte Version findet ihr im EmpyrionBaseAlign/bin Verzeichnis falls ihr die Mod nicht selber prüfen und compilieren wollt ;-)

### What is this?

Es soll dazu dienen eine neue Basis (AlignBase) so auszurichten und zu positionieren das sie im Rastermaß einer bestehenden Basis (MainBase) liegt und beim Bauen nahtlos an diese anschließt

#### What are all the commands?

Hinweis: Zur Zeit ist implementiert das ein Spieler nur eine Basis ausrichten kann die nicht mehr als 10 Blöcke hat, also direkt nach dem Setzen des Basisstarter. 
Damit werden viele Exploids vermieden die sich sonst ergeben würden. Spieler mit einer höheren Berechtigung dürfen alles Ausrichten.

#### Help

* /al : Zeigt die Kommandos der Mod an

#### Align

* /al {BaseToAlignId} {MainBaseId}
* /al {BaseToAlignId} {MainBaseId} {ShiftX},{ShiftY},{ShiftZ}
* /al {BaseToAlignId} 0

Richtet die Basis (BaseToAlignId) so aus und positioniert sie das sie im Rastermaß einer bestehenden Basis (MainBaseId) liegt und beim Bauen nahtlos an diese anschließt.

Mit dem {ShiftXYZ} kann noch eine Verschiebung wärend der Ausrichtung angegeben werden um die auszurichtene Basis im neuen Rastermaß zu verschieben.

UNDO: Wenn die {MainBaseId} = 0 ist wird die Basis {BaseToAlignId} wieder an die ursprüngliche Position und Ausrichtung zurückgesetzt werden.

### Restore?
Im Logfile werden die 'setposition' und 'setrotation' Kommandos hinterlegt die zur Restaurierung der 'alten' Position und Ausrichtung genutzt werden können

### Is that it?
Zunächst erstmal und damit viel Spaß beim natlosen Bauen wünscht euch

ASTIC/TC