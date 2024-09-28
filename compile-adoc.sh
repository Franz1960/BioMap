#!/bin/bash

BUILDDIR=build

# Common flags for all asciidoctor invocations
ASCIIDOCTOR_FLAGS=( "-d article" "-a sectlinks" "--failure-level=WARN" "-r asciidoctor-diagram" )

# Additional flags for the html invocation
# Use `data-uri` to embed images as base64 in html.
ASCIIDOCTOR_HTML_FLAGS=( "-b html5" "-a data-uri" )
# Additional flags for the PDF invocation
ASCIIDOCTOR_PDF_FLAGS=( "-a pdf-themesdir=Organisation/Formatierung/pdf-themes" )

die() {
	echo $0 failed: $*
	exit 1
}

which parallel >/dev/null 2>&1 || die 'Cannot find `parallel`'

find . -type f -name \*.adoc | parallel --will-cite asciidoctor ${ASCIIDOCTOR_FLAGS[@]} ${ASCIIDOCTOR_HTML_FLAGS[@]} -o ${BUILDDIR}/html/'{.}'.html '{}' || die "Error generating HTML output"
