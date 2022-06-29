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
      var isAdminMode = conf == null || typeof (conf.isAdminMode) === 'undefined' ? false : conf.isAdminMode;
      var buttons;
      var plugins;

      if (isAdminMode)
      {
        buttons = [
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
        ];
        plugins = {
          insertNucleusImage: {},
          insertNucleusPage: {}
        };
      }
      else
      {
        buttons = [
          ['historyUndo', 'historyRedo'],
          ['formatting'],
          ['strong', 'em'],
          ['link'],
          ['justifyLeft', 'justifyCenter', 'justifyRight', 'justifyFull'],
          ['unorderedList', 'orderedList'],
          ['horizontalRule'],
          ['removeformat']
        ];
        plugins = {};
      }

      var htmlEditor = jQuery(value).trumbowyg({
        btns: buttons,
        semantic: true,
        removeformatPasted: true,
        tagsToRemove: ['script', 'link'],
        imageWidthModalEdit: true,
        plugins: plugins,        
        svgPath: document.baseURI + "Resources/Libraries/HtmlEditors/Trumbowyg/02.25.01/ui/icons.svg"
      });

      // this is a workaround.  contenteditable divs don't always get focus on click in Edge, especially in popups
      htmlEditor.siblings('.trumbowyg-editor').on('click', function ()
      {
        jQuery(this).focus(); return false;
      });
    });
  }
})(jQuery);