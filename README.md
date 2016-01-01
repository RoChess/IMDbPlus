

# Introduction #

IMDb+ scraper enables English titles, RottenTomatoes ratings, title grouping and many other features.

Minimum Requirements IMDb+ Plugin:
  * MediaPortal v1.2.x
  * MovingPictures v1.2.x

Donations are welcome:

[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=4DDQ6GBDGJRK6)

and much appreciated!!


---


# Installation #

Please use the 'Downloads' tab to get the MPEI package for the IMDb+ plugin. It is also easy to install it via the 'MediaPortal Extension Installer' shortcut on your desktop (if the IMDb+ plugin is not listed under 'Known Extensions', then go to the Options tab and manually update the MPEI list first). This will add the IMDb+ plugin to your MediaPortal installation. The next time you then start MediaPortal the IMDb+ plugin will check to see if the IMDb+ scraper script is installed in MovingPictures.

It will automatically install the latest IMDb+ scraper script and configure it as the primary source of your movies. Every 24 hours it will check to see if a new version has been released and automatically upgrade it for you. All these options will be configurable in the future, but for now it is set to full auto.

Please see the InstallPlugin wiki entry for more information.


---


# Why do I need this #

If you use Moving-Pictures and want all your movie titles in English, then this is 'the' scraper for you. It is the first scraper to support custom options, allowing you to change the way this scraper works.

The most favorite feature of this scraper is not only to force English titles, but grouping series together. This is best shown visually, so on the left is the default imdb.com scraper result and on the right is the IMDb+ result with the rename option enabled:

| **Default** | **IMDb+** |
|:------------|:----------|
| Rename option disabled (results same as default scraper) | Rename option enabled |
| ![http://blixy.net/mp/MovPic.Scraper.Default.png](http://blixy.net/mp/MovPic.Scraper.Default.png) | ![http://blixy.net/mp/MovPic.Scraper.IMDb+.png](http://blixy.net/mp/MovPic.Scraper.IMDb+.png) |


And now you can use the new plugin to adjust all the options from within MediaPortal v1.2.x GUI.

![http://blixy.net/mp/Plugin.IMDb+.jpg](http://blixy.net/mp/Plugin.IMDb+.jpg)

The scraper forces English title on foreign movies. For example the movie **[Chin gei bin (2003)](http://www.imdb.com/title/tt0351887/)** will be imported as 'Vampire Effect'.

  * Enable 'Original Title' option via the IMDb+ plugin to import the same movie as "Chin gei bin".
  * Enable 'Add Foreign Title' option for "Vampire Effect (Chin gei bin)".
  * Enable 'Foreign Title First' option for "Chin gei bin (Vampire Effect)".

To install this scraper in Moving-Pictures, please follow the instructions at **WikiInstallScraper**.


---

# Ratings #

The IMDb+ Scraper supports a wide range of movie rating systems.

  * IMDb score
  * Metacritics Metascore
  * RottenTomatoes Audience Average rating
  * RottenTomatoes Audience Percentile rating
  * RottenTomatoes All Critics Average rating
  * RottenTomatoes All Critics Percentile rating
  * RottenTomatoes Top Critics Average rating
  * RottenTomatoes Top Critics Percentile rating

_Note: each rating is adjusted to match the 10-score system that Moving-Pictures uses. So both 78% and 3.9/5 result in a 7.8 score._

Usually it is easy to compare one of those rating systems to your own personal taste and configure the IMDb+ scraper to use just that one. But on examining the results of a large selection of movies, it became clear that none of the single rating systems are ideal.

For example movies that are just released often times have a score that is based on votes from people behind-the-scenes of that movie. For this reason an additional option exists to not even take the IMDb score into account when there are only a few votes (the 'Min. IMDb Votes' setting).

To make the rating system easier to use, it was decided to add an average scoreing system.

By default the average score of IMDb, Metacritics, RottenTomatoes All Critics Average, RottenTomatoes Top Critics Average and RottenTomatoes Audience Average is used. If none of those contain a rating, then the RottenTomatoes Audience Percentile score is used. The reason both 'All' and 'Top' critics scores are used, is to balance out the community scores of IMDb and RottenTomatoes Audience.

If 'Single Score' is disabled and the 'IMDb Score' setting is enabled then the average score of IMDb + Metacritics is used.


---

# Additional Skins #

More and more skins are adding support for the IMDb+ plugin, such as the popular aMPed and StreamedMP skins.

<img src='http://blixy.net/mp/Plugin.IMDb+.aMPed.jpg' width='360' height='288' />
_(aMPed)_

![http://blixy.net/mp/Plugin.IMDb+.StreamedMP.jpg](http://blixy.net/mp/Plugin.IMDb+.StreamedMP.jpg)
_(StreamedMP)_

![http://blixy.net/mp/Plugin.IMDb+.PureVisionHD.jpg](http://blixy.net/mp/Plugin.IMDb+.PureVisionHD.jpg)
_(PureVisionHD)_

It could however happen that some very new features of the plugin are not yet supported. You could temporary copy the Default or DefaultWide skin files over to your active skin folder, or simply give the skin developer a little bit of time to update their skins.

## Skin Extras ##

Some of the additional skins offer tighter integration with MovingPictures, and you can access the IMDb+ plugin directly from their respective hidden menus while inside MovingPictures.

![http://blixy.net/mp/Plugin.IMDb+.StreamedMP.MovPic.Integration.jpg](http://blixy.net/mp/Plugin.IMDb+.StreamedMP.MovPic.Integration.jpg)

Above is an example of how this looks like with the popular StreamedMP skin.
