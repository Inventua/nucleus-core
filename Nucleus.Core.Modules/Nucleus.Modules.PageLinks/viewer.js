/* =============================================================
 * Plug-in that creates a list of jump links based on the jQuery 
 * selector used as the root for detected elements.
 * =============================================================
 */
(function ($)
{
  jQuery.fn.PageLinkViewer = function (conf)
  {
    let options = jQuery.extend({}, conf);

    // private variables

    // private functions

    // create and return the page links list 
    function _buildPageLinksList(rootElement, includedHeaders, headingClass)
    {
      let headerElements = rootElement.find(':header:visible');
      let pageLinkslistElement = jQuery('<div></div>');
      let headerIndex = 1;

      if (headerElements.length > 0)
      {
        let currentHeaderIndex = -1;
        let queuedPageLinks = [];
          
        for (let headersIndex = 0; headersIndex < headerElements.length; headersIndex++)
        {
          let headerElement = headerElements[headersIndex];

          if (_shouldIncludeHeader(headerElement, includedHeaders, headingClass))
          {
            headerIndex = parseInt(headerElement.nodeName.substring(1), 10);
          }

          // render a child list on change of level
          if (headerIndex !== currentHeaderIndex)
          {
            if (queuedPageLinks.length !== 0)
            {
              let parentListItem = _getParentListItem(headerElements, pageLinkslistElement, jQuery(headerElements[headersIndex-1]), includedHeaders, headingClass);
              parentListItem.append(parentListItem.is('li') ? jQuery('<ol></ol>').append(queuedPageLinks): queuedPageLinks);
              queuedPageLinks = [];
            }
            currentHeaderIndex = headerIndex;
          }

          if (_shouldIncludeHeader(headerElement, includedHeaders, headingClass))
          {
            let pageLinkItem = _buildPageLink(jQuery(headerElement));
            queuedPageLinks.push(pageLinkItem);            
          }
          
          // render a child list after processing the last heading in the list
          if (headersIndex === headerElements.length - 1)
          {
            let parentListItem = _getParentListItem(headerElements, pageLinkslistElement, jQuery(headerElement), includedHeaders, headingClass);
            parentListItem.append(parentListItem.is('li') ? jQuery('<ol></ol>').append(queuedPageLinks) : queuedPageLinks);
          }
        }
      }

      return pageLinkslistElement.children();
    }

    // create and return a list item containing an anchor <a> element which links to the specified element 
    function _buildPageLink(element)
    {
      let elementId = element.prop('id');

      let relativeUrl = new URL(window.location.href);
      relativeUrl.hash = '#' + elementId;

      let linkText = jQuery(element).attr('data-title');
      if (typeof linkText === 'undefined')
      {
        linkText = jQuery(element).text().trim();
      }

      let listElement = jQuery('<li></li>');
      let linkElement = jQuery('<a href="#">' + linkText + '</a>');
      if (elementId === '')
      {
        // if the header element does not have an id, set the PageLinksTarget property and attach an event handler to 
        // manage scrollTo.
        linkElement[0].PageLinksTarget = element;
        linkElement.on('click', _scrollTo);
      }
      else
      {
        // if the header element has an id, use it
        linkElement.attr('href', relativeUrl.toString());
        // still call _scrollTo to manage the header animation
        linkElement.on('click', _scrollTo);
      }

      listElement.append(linkElement);

      return listElement;
    }

    // return the parent of the list item which is linked to the specified element
    function _getParentListItem(headerElements, listElement, element, includedHeaders, headingClass)
    {
      // first item is always added to the top-level list
      if (headerElements[0] === element[0])
      {
        return listElement;
      }
      else      
      {
        let thisHeaderLevel = parseInt(element[0].nodeName.substring(1), 10);
        // loop through preceeding headers in reverse to find a header level less than the specified element
        for (let headersCount = headerElements.index(element); headersCount >= 0; headersCount--)
        {
          let headerElement = headerElements[headersCount];
          let headerLevel = parseInt(headerElement.nodeName.substring(1), 10);

          if (_shouldIncludeHeader(headerElement, includedHeaders, headingClass) && headerLevel < thisHeaderLevel)
          {
            // find the list item which is linked to the "parent" header element
            let parentListItem = null;
      
            // handle headers which are linked by id
            let relativeUrl = new URL(window.location.href);
            relativeUrl.hash = '#' + jQuery(headerElement).prop('id');
            parentListItem = jQuery(listElement).find('a[href*="' + relativeUrl.toString() + '"]').parent();

            if (parentListItem.length !== 0) return parentListItem;

            if (parentListItem === null || parentListItem.length === 0)
            {
              // handle headers which are linked by the PageLinksTarget property
              let links = jQuery(listElement).find('a');
              for (let childCount = links.length-1; childCount >= 0; childCount--)
              {
                if (typeof links[childCount].PageLinksTarget !== 'undefined' && links[childCount].PageLinksTarget.length > 0 && links[childCount].PageLinksTarget[0] === headerElement)
                {
                  return jQuery(links[childCount]).parent();
                }
              }
            }
          }
        }
      }

      return listElement;
    }
    
    // return whether the header element should be included in the page links list, based on module settings
    function _shouldIncludeHeader(element, includedHeaders, headingClass)
    {
      let doIncludeHeader = includedHeaders.toUpperCase().split(',').includes(element.nodeName);

      if (doIncludeHeader)
      {
        if (headingClass === '') return true;

        let headingClassArray = headingClass.split(',');
        for (let index = 0; index < headingClassArray.length; index++)
        {
          if (jQuery(element).hasClass(headingClassArray[index].trim().replace(/^\./gm, ''))) return true;
        }
      }
      return false;
    }

    // for the clicked page link, scroll the linked header into view 
    function _scrollTo(event)
    {
      let scrollTargetElement = this.PageLinksTarget;

      if (typeof scrollTargetElement === 'undefined' || scrollTargetElement === null)
      {
        // if PageLinksTarget is not set, the header element has an id, use it to find the target element so
        // we can highlight it
        let relativeUrl = new URL(jQuery(this).attr('href'))
        scrollTargetElement = jQuery(relativeUrl.hash);
      }
      else
      {
        // if we are using the PageLinksTarget property, then we need to suppress default behavior
        event.preventDefault();
      }

      let durationValue = getComputedStyle(scrollTargetElement[0]).getPropertyValue('--page-links-highlight-duration');
      let duration = Number.isNaN(parseInt(durationValue)) ? 0 : parseInt(durationValue);

      if (duration > 0)
      {
        scrollTargetElement
          .removeClass('pagelink-highlight')
          .addClass('pagelink-highlight');
      }

      scrollTargetElement[0].scrollIntoView({ block: "center", inline: "nearest" });

      if (duration !== '')
      {
        window.setTimeout(function ()
        {
          scrollTargetElement.removeClass('pagelink-highlight');
        }, duration);
      }
    }

    // build the page links list
    return this.each(function ()
    {
      let pageLinksWrapper = jQuery(this);
      if (pageLinksWrapper[0].hasAttribute('data-page-links-rootselector'))
      {
        let rootSelector = pageLinksWrapper.attr('data-page-links-rootselector');
        let includedHeaders = pageLinksWrapper.attr('data-page-links-includeheaders');
        let headingClass = pageLinksWrapper.attr('data-page-links-heading-class');

        // remove the attributes because they are only needed until this code has executed. Also removing them prevents the plugin from
        // running twice on the same wrapper.
        pageLinksWrapper.removeAttr('data-page-links-rootselector');
        pageLinksWrapper.removeAttr('data-page-links-includeheaders');
        pageLinksWrapper.removeAttr('data-page-links-heading-class');

        if (!rootSelector || rootSelector.length === 0)
        {
          rootSelector = 'body';
        }

        pageLinksWrapper.append(_buildPageLinksList(jQuery(rootSelector), includedHeaders, headingClass));
      }
    })
  };
}(jQuery));