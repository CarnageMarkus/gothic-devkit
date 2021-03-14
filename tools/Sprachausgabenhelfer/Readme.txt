
********************************************
**********Sprachausgabenhelfer 2.0**********
**********Programmiert von Caphalor*********
*******************Readme*******************
********************************************

Mit dem Sprachausgabenhelfer l�sst sich herausfinden, welche Sounddateien eines Modprojektes noch fehlen. Dazu zieht man einfach die entsprechenden Scripte in eine Box, gibt den Pfad der .Wav-Dateien an und klickt auf "Abgleich starten". In einer zweiten Box werden dann die fehlenden Sounddateien angezeigt.
Die Liste kann dann in eine Datei geschrieben werden.


***Neue Funktionen des Sprachausgabenhelfers in Version 2.1***

1. .src-Unterst�tzung
Es k�nnen nun auch .src zur Liste hinzugef�gt werden. Alle in der .src-Datei referenzierten Dateien werden der Liste hinzugef�gt.

2. Output-Units erstellen (Makro)
Basierend auf der .src-Unterst�tzung habe ich eine Makro Funktion erstellt, die es erlaubt, mit einem einzigen Klick die OUs zu aktualisieren. Dazu ist (fast) nicht die gringste Vorarbeit n�tig. Ihr braucht keine besondere Ini, m�sst keine Dateien l�schen (ihr d�rft es sogar nicht) und vor allen Dingen m�sst ihr den Spacer nicht starten. ;-)
Der Pfad der Gothic-Installation muss aber vorher unter "Einstellungen" eingestellt werden (nur einmalig notwendig).

3. Dialog zu Script
Siehe hier: http://gv.at.serveftp.net/forum/showthread.php?t=39

Die �nderungen stehen nur im gro�en Fenstermodus zur Verf�gung.




***Neue Funktionen des Sprachausgabenhelfers in Version 2.0***

1. OUINFO.INF aktualisieren
Dieser Button ist erst anklickbar, wenn sich .d-Scripte in der Box befinden. Aus diesen Scripten wird bei Klick auf diesen Button eine OuInfo.Inf erstellt. Diese wird im Verzeichnis des Sprachausgabenhelfers oder abgelegt.
Unter "Einstellungen" kann man optional einstellen, dass die OUINFO.Inf direkt im Gothic-Verzeichnis ersetzt wird.


2. OU.CSL aktualisieren
Aus der OuInfo.Inf und aus der SVM.d wird eine OU.CSL erstellt. Die Datei wird im Verzeichnis des Sprachausgabenhelfers gespeichert.
Unter "Einstellungen" kann man optional einstellen, dass OU.CSL direkt im Gothic-Verzeichnis ersetzt wird. In diesem Fall wird auch die OU.BIN im Gothic-Verzeichnis gel�scht, damit Gothic beim n�chsten Start diese aus der erstellten OU.CSL aktualisiert.


3. Fensterlayout �ndern
Unter Einstellungen l�sst sich das Fensterlyout �ndern. Standardm��ig ist das gro�e Fenster eingestellt. Der Sprachausgabenhelfer bietet aber auch ein handlicheres kleines Fenster, welches trotzdem alle Funktionen des gro�en bietet (vielen Dank an Gothicfan359 f�r die Vorlage des kleinen Fensters).
Zus�tzlich l�sst sich noch die Fensterfarbe �ndern.


4. Allgemeines �ber das Dateiformat ".Gof"
Der Sprachausgabenhelfer kann .Gof-Dateien importieren und exportieren. In .Gof-Dateien werden Informationen �ber die fehlenden Sounddateien gespeichert, die der Sprachausgabenhelfer gefunden hat. (Zum Beispiel Dateiname, Text, Npc usw.)
Im - und Export von .Gof-Dateien wird auch vom Output-Commander von Lubo unterst�tzt. Das erm�glicht einen Informationsaustausch zwischen dem Sprachausgabenhelfer und dem Output-Commander.

	4.1 Import .Gof
	Eine .Gof-Datei wird importiert. Diese Funktion l�sst sich �ber "Werkzeuge -> Import .Gof" aufrufen. Sie wird aber auch 		aufgerufen, wenn man eine .Gof-Datei in die Box zieht.

	4. 2 Export .Gof
	Diese Funktion ist erst verf�gbar, wenn fehlende Sounddateien gefunden wurden. Aus diesen wird eine .Gof-Datei erstellt. 		Diese Funktion kann man �ber Werkzeuge -> .Gof Export aufrufen. Erstellt man eine Textdatei der fehlenden Sounddateien 			(Dateiexport), so wird automatisch eine Sprachausgabenhelfer.Gof exportiert.


5. Dialoge korrigieren
Mit dem Sprachausgabenhelfer lassen sich Dialoge bequem und unkompliziert korrigieren. Die Funktion listet alle Sounddateien, die in einem Script refenziert werden, in einer Liste auf. Makiert man einen der Listeneintr�ge, wird der Untertitel der Sounddatei angezeigt. Den kann man dann frei ver�ndern. Bei Klick auf "�bernehmen" wird das Script gespeichert.
Im gro�en Fenster l�sst sich die Funktion aufrufen, indem man ein in die Box gezogenes Script markiert und auf "Dialoge korrigieren..." klickt. In beiden Fenstern kann man aber auch ganz einfach einen Doppelklick auf ein in die Box gezogenes Script machen, und das "Dialoge korrigieren..."-Fenster wird ge�ffnet.


6. Unterst�tzung der SVM.d
Die SVM.d kann wie gew�hnliche Dialogscripte in die Box gezogen werden, und kann mit den vorhandenen Sounddateien abgeglichen werden.


Das wars eigentlich auch schon. Bei Fragen, Kritik oder Bugberichten bitte einfach eine PN im worldofgothic Forum an Caphalor. ;)
