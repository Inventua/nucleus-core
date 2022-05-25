// jQuery.fn.HtmlEditor = jQuery.fn.trumbowyg;
// https://alex-d.github.io/Trumbowyg/documentation/#disabled
(function ($)
{
  jQuery.fn.HtmlEditor = function (conf)
  {
    jQuery.trumbowyg.svgAbsoluteUseHref = true;
    // For every element passed to the plug-in
    return this.each(function (index, value)
    {
      var htmlEditor = jQuery(value).trumbowyg({
        btns: [
          ['viewHTML'],
          ['historyUndo', 'historyRedo'],
          ['formatting'],
          ['strong', 'em'], 
          ['link'],
          ['insertNucleusPage'],
          ['insertNucleusImage'],
          ['justifyLeft', 'justifyCenter', 'justifyRight', 'justifyFull'],
          ['unorderedList', 'orderedList'],
          ['horizontalRule'],
          ['removeformat']
        ],
        semantic: true,
        removeformatPasted: true,
        tagsToRemove: ['script', 'link'],
        imageWidthModalEdit: true,
        plugins: {
          insertNucleusImage: {},
          insertNucleusPage: {}
        },        
        svgPath: document.baseURI + "Resources/Libraries/Trumbowyg/ui/icons.svg"
      });

      // this is a workaround.  contenteditable divs don't always get focus on click in Edge, especially in popups
      htmlEditor.siblings('.trumbowyg-editor').on('click', function ()
      {
        jQuery(this).focus(); return false;
      });
    });
  }
})(jQuery);