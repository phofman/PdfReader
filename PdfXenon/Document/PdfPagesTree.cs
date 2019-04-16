﻿using System;
using System.Collections.Generic;

namespace PdfXenon.Standard
{
    public class PdfPagesTree : PdfPages
    {
        private List<PdfPage> _pages;

        public PdfPagesTree(PdfDocument doc, PdfCatalog catalog, ParseDictionary dictionary)
            : base(doc, null, dictionary)
        {
            Catalog = catalog;
        }

        public PdfCatalog Catalog { get; private set; }

        public PdfPage this[int index]
        {
            get { return Pages[index]; }
        }

        private List<PdfPage> Pages
        {
            get
            {
                if (_pages == null)
                {
                    _pages = new List<PdfPage>();
                    CreatePages(Doc, _pages);
                }

                return _pages;
            }
        }
    }
}
