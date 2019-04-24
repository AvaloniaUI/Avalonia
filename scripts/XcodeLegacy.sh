#!/bin/bash
# XCodeLegacy.sh
#
# Original author: Frederic Devernay <frederic.devernay@m4x.org>
# Contributors:
# - Garrett Walbridge <gwalbridge+xcodelegacy@gmail.com>
# - Jae Liu <ling32945@github>
# - Eric Knibbe <EricFromCanada@github>
# - Chris Roueche <croueche@github>
# - Kris Coppieters <zwettemaan@github>
# - Nick Beadman <nbeadman@gmail.com> / <nbeadman@github>
#
# License: Creative Commons BY-NC-SA 3.0 http://creativecommons.org/licenses/by-nc-sa/3.0/
#
# History:
# 1.0 (08/10/2012): First public version, supports Xcode up to version 4.6.3
# 1.1 (20/09/2013): Xcode 5 removed llvm-gcc and 10.7 SDK support, grab them from Xcode 3 and 4
# 1.2 (03/02/2014): Xcode 5 broke PPC assembly and linking; fix assembly and grab linker from Xcode 3
# 1.3 (07/10/2014): Xcode 6 removed 10.8 SDK, grab it from Xcode 5.1.1
# 1.4 (21/08/2015): Xcode 7 removed 10.9 and 10.10 SDKs, grab them from Xcode 6.4
# 1.5 (15/10/2015): Fixes for OS X 10.11 El Capitan (nothing can be installed in /usr/bin because of the sandbox)
# 1.6 (11/11/2015): Fix buildpackages, fix /usr/bin/gcc on recent OS X, fix download messages
# 1.7 (05/04/2016): Xcode 7.3 disables support for older SDKs, fix that
# 1.8 (07/04/2016): add options to install only some SDKs or compilers only
# 1.9 (16/09/2016): Xcode 8 dropped 10.11 SDK, get it from Xcode 7.3.1
# 2.0 (02/05/2017): Xcode 8 cannot always link i386 for OS X 10.5, use the Xcode 3 linker for this arch too. Force use of legacy assembler with GCC 4.x.
# 2.1 (17/01/2017): Xcode 9 dropped 10.12 SDK, get it from https://github.com/phracker/MacOSX-SDKs; fix compiling with GNU Ada, and many other fixes
# 2.2 (12/02/2019): Added support for using macOS High Sierra 10.13 SDK from Xcode 9.4.1 for use on Xcode 10/macOS 10.14 Mojave, also changed source of OS X 10.12 SDK to Xcode 8.3.3

#set -e # Exit immediately if a command exits with a non-zero status
#set -u # Treat unset variables as an error when substituting.
#set -x # Print commands and their arguments as they are executed.

compilers=0
osx104=0
osx105=0
osx106=0
osx107=0
osx108=0
osx109=0
osx1010=0
osx1011=0
osx1012=0
osx1013=0
gotoption=0
error=0

while [[ $error = 0 ]] && [[ $# -gt 1 ]]; do

    case $1 in
        -compilers)
            compilers=1
            gotoption=1
            shift
            ;;
        -osx104)
            osx104=1
            gotoption=1
            shift
            ;;
        -osx105)
            osx105=1
            gotoption=1
            shift
            ;;
        -osx106)
            osx106=1
            gotoption=1
            shift
            ;;
        -osx107)
            osx107=1
            gotoption=1
            shift
            ;;
        -osx108)
            osx108=1
            gotoption=1
            shift
            ;;
        -osx109)
            osx109=1
            gotoption=1
            shift
            ;;
        -osx1010)
            osx1010=1
            gotoption=1
            shift
            ;;
        -osx1011)
            osx1011=1
            gotoption=1
            shift
            ;;
        -osx1012)
            osx1012=1
            gotoption=1
            shift
            ;;
        -osx1013)
            osx1013=1
            gotoption=1
            shift
            ;;
        *)
            # unknown option or spurious arg
            error=1
            ;;
    esac

done

if [ $gotoption = 0 ]; then
    compilers=1
    osx104=1
    osx105=1
    osx106=1
    osx107=1
    osx108=1
    osx109=1
    osx1010=1
    osx1011=1
    osx1012=1
    osx1013=1
fi

if [ $# != 1 ]; then
    #     ################################################################################ 80 cols
    echo "Usage: $0 [-compilers|-osx104|-osx105|-osx106|-osx107|-osx108|-osx109|-osx1010|-osx1011|-osx1012|-osx1013] buildpackages|install|installbeta|cleanpackages|uninstall|uninstallbeta"
    echo ""
    echo "Description: Extracts / installs / cleans / uninstalls the following components"
    echo "from Xcode 3.2.6, Xcode 4.6.3, Xcode 5.1.1, Xcode 6.4, Xcode 7.3.1, Xcode 8.3.3 and Xcode 9.4.1 which"
    echo "are not available in Xcode >= 4.2:"
    echo " - PPC assembler and linker"
    echo " - GCC 4.0 and 4.2 compilers and Xcode plugins"
    echo " - LLVM-GCC 4.2 compiler and Xcode plugin (Xcode >= 5)"
    echo " - Mac OS X SDK 10.4u, 10.5, 10.6, 10.7, 10.8, 10.9, 10.10, 10.11, 10.12, 10.13"
    echo ""
    echo "An optional first argument may be provided to limit the operation (by default"
    echo "everything is done):"
    echo " -compilers : only install the gcc and llvm-gcc compilers, as well as the"
    echo "              corresponding Xcode plugins"
    echo " -osx104    : only install Mac OSX 10.4 SDK"
    echo " -osx105    : only install Mac OSX 10.5 SDK"
    echo " -osx106    : only install Mac OSX 10.6 SDK"
    echo " -osx107    : only install Mac OSX 10.7 SDK"
    echo " -osx108    : only install OSX 10.8 SDK"
    echo " -osx109    : only install OSX 10.9 SDK"
    echo " -osx1010   : only install OSX 10.10 SDK"
    echo " -osx1011   : only install OSX 10.11 SDK"
    echo " -osx1012   : only install OSX 10.12 SDK"
    echo " -osx1013   : only install OSX 10.13 SDK"
    echo "Note that these can be combined. For example, to build and install the 10.9"
    echo "and 10.10 SDKs, one could execute:"
    echo " $ $0 -osx109 -osx1010 buildpackages"
    echo " $ sudo $0 -osx109 -osx1010 install"
    echo ""
    echo "Typically, you will want to run this script with the buildpackages argument"
    echo "first, then the install argument, and lastly the cleanpackages argument, in"
    echo "order to properly install the legacy Xcode files."
    echo "The install and uninstall phases have to be run with administrative rights, as"
    echo "in:"
    echo " $ sudo $0 install"
    echo "The installbeta and uninstallbeta phases work on the beta versions of Xcode."
    exit
fi

if [ "$1" = "installbeta" ] || [ "$1" = "uninstallbeta" ]; then
    XCODEAPP="/Applications/Xcode-beta.app"
else
    XCODEAPP="/Applications/Xcode.app"
fi
XCODE42=0
PLUGINDIR="$XCODEAPP/Contents/PlugIns/Xcode3Core.ideplugin/Contents/SharedSupport/Developer/Library/Xcode/Plug-ins"
GCCDIR="$XCODEAPP/Contents/Developer"
SDKDIR="$GCCDIR/Platforms/MacOSX.platform/Developer"
if [ -d "$XCODEAPP" ]; then
    echo "*** Info: found Xcode >= 4.3 in $XCODEAPP"
else
    GCCDIR="/Developer"
    XCODEAPP="$GCCDIR/Applications/Xcode.app"
    PLUGINDIR="$GCCDIR/Library/Xcode/PrivatePlugIns/Xcode3Core.ideplugin/Contents/SharedSupport/Developer/Library/Xcode/Plug-ins"
    SDKDIR="$GCCDIR"
    if [ -d "$XCODEAPP" ]; then
        XCODE42=1
        echo "*** Info: found Xcode <= 4.2.1 in $XCODEAPP"
    else
        echo "*** Info: could not find Xcode 4.2 in /Developer/Applications nor Xcode >= 4.3 in /Applications"
    fi
fi
PLATFORMDIR="$GCCDIR/Platforms/MacOSX.platform"
GCCINSTALLDIR="$GCCDIR/Toolchains/XcodeDefault.xctoolchain"
GCCLINKDIR=/usr
RELEASENUM=$(uname -r | awk -F. '{print $1}')
if [ "$RELEASENUM" -gt 14 ]; then
    # on OSX 10.11 El Capitan, nothing can be installed in /usr because of the Sandbox
    # install in Xcode instead, and put links in /usr/local
    GCCLINKDIR=/usr/local
elif [ "$RELEASENUM" -lt 10 ]; then
    echo "*** Error: This script requires Mac OS X 10.6 Snow Leopard or newer."
    exit 1
fi

GCCFILES="usr/share/man/man7/fsf-funding.7 usr/share/man/man7/gfdl.7 usr/share/man/man7/gpl.7 usr/share/man/man1/*-4.0.1 usr/share/man/man1/*-4.0.1.1 usr/libexec/gcc/*-apple-darwin10/4.0.1 usr/lib/gcc/*-apple-darwin10/4.0.1 usr/include/gcc/darwin/4.0 usr/bin/*-4.0 usr/bin/*-4.0.1 usr/share/man/man1/*-4.2.1 usr/share/man/man1/*-4.2.1.1 usr/libexec/gcc/*-apple-darwin10/4.2.1 usr/lib/gcc/*-apple-darwin10/4.2.1 usr/include/gcc/darwin/4.2 usr/bin/*-4.2 usr/bin/*-4.2.1"
LLVMGCCFILES="usr/llvm-gcc-4.2 usr/share/man/man1/llvm-g*.1.gz"

xc3="$(( compilers + osx104 + osx105 + osx106 != 0 ))"
xc4="$(( compilers + osx107 != 0 ))"
xc5="$(( osx108 != 0 ))"
xc6="$(( osx109 + osx1010 != 0 ))"
xc7="$(( osx1011 != 0 ))"
xc8="$(( osx1012 != 0 ))"
xc9="$(( osx1013 != 0 ))"

# The sole argument is the macOS version (e.g. 10.12)
installSDK() {
    macos="$1"
    macosnodot="${macos//./}"
    if [ -d "$SDKDIR/SDKs/MacOSX${macos}.sdk" ]; then
        echo "*** Not installing MacOSX${macos}.sdk (found installed in $SDKDIR/SDKs/MacOSX${macos}.sdk, uninstall first to force install)"
    else
        if [ -f Xcode${macosnodot}SDK.tar.gz ]; then
            (gzip -dc Xcode${macosnodot}SDK.tar.gz | (cd "$SDKDIR" || exit; tar xf -)) && echo "*** installed Xcode${macosnodot}SDK.tar.gz"
        elif [ -f MacOSX${macos}.sdk.tar.xz ]; then
            (gzip -dc MacOSX${macos}.sdk.tar.xz | (cd "$SDKDIR/SDKs" || exit; tar xf -)) && echo "*** installed MacOSX${macos}.sdk.tar.xz"
        else
            echo "*** Could not install MacOSX${macos}.sdk"
            echo "*** Before installing:"
            echo "- execute \"$0 buildpackages\""
            exit 1
        fi
        touch "$SDKDIR/SDKs/MacOSX${macos}.sdk/legacy"
    fi
}

case $1 in
    buildpackages)
        #######################
        # PHASE 1: PACKAGING
        #
        missingdmg=0
        # note: Xcode links from http://stackoverflow.com/questions/10335747/how-to-download-xcode-4-5-6-7-and-get-the-dmg-file/10335943#10335943
        if [ "$xc3" = 1 ] && [ ! -f xcode_3.2.6_and_ios_sdk_4.3.dmg ]; then
            echo "*** You should download Xcode 3.2.6. Login to:"
            echo " https://developer.apple.com/downloads/"
            echo "then download from:"
            echo " https://developer.apple.com/devcenter/download.action?path=/Developer_Tools/xcode_3.2.6_and_ios_sdk_4.3__final/xcode_3.2.6_and_ios_sdk_4.3.dmg"
            echo "or"
            echo " https://adcdownload.apple.com/Developer_Tools/xcode_3.2.6_and_ios_sdk_4.3__final/xcode_3.2.6_and_ios_sdk_4.3.dmg"
            echo "and then run this script from within the same directory as the downloaded file"
            missingdmg=1
        fi
        if [ "$xc4" = 1 ] && [ ! -f xcode4630916281a.dmg ]; then
            echo "*** You should download Xcode 4.6.3. Login to:"
            echo " https://developer.apple.com/downloads/"
            echo "then download from:"
            echo " https://developer.apple.com/devcenter/download.action?path=/Developer_Tools/xcode_4.6.3/xcode4630916281a.dmg"
            echo "or"
            echo " https://adcdownload.apple.com/Developer_Tools/xcode_4.6.3/xcode4630916281a.dmg"
            echo "and then run this script from within the same directory as the downloaded file"
            missingdmg=1
        fi
        if [ "$xc5" = 1 ] && [ ! -f xcode_5.1.1.dmg ]; then
            echo "*** You should download Xcode 5.1.1. Login to:"
            echo " https://developer.apple.com/downloads/"
            echo "then download from:"
            echo " https://developer.apple.com/devcenter/download.action?path=/Developer_Tools/xcode_5.1.1/xcode_5.1.1.dmg"
            echo "or"
            echo " https://adcdownload.apple.com/Developer_Tools/xcode_5.1.1/xcode_5.1.1.dmg"
            echo "and then run this script from within the same directory as the downloaded file"
            missingdmg=1
        fi
        if [ "$xc6" = 1 ] && [ ! -f Xcode_6.4.dmg ]; then
            echo "*** You should download Xcode 6.4. Login to:"
            echo " https://developer.apple.com/downloads/"
            echo "then download from:"
            echo " https://developer.apple.com/devcenter/download.action?path=/Developer_Tools/Xcode_6.4/Xcode_6.4.dmg"
            echo "or"
            echo " https://adcdownload.apple.com/Developer_Tools/Xcode_6.4/Xcode_6.4.dmg"
            echo "and then run this script from within the same directory as the downloaded file"
            missingdmg=1
        fi
        if [ "$xc7" = 1 ] && [ ! -f Xcode_7.3.1.dmg ]; then
            echo "*** You should download Xcode 7.3.1. Login to:"
            echo " https://developer.apple.com/downloads/"
            echo "then download from:"
            echo " https://developer.apple.com/devcenter/download.action?path=/Developer_Tools/Xcode_7.3.1/Xcode_7.3.1.dmg"
            echo "or"
            echo " https://adcdownload.apple.com/Developer_Tools/Xcode_7.3.1/Xcode_7.3.1.dmg"
            echo "and then run this script from within the same directory as the downloaded file"
            missingdmg=1
        fi
        if [ "$xc8" = 1 ] && [ ! -f Xcode8.3.3.xip ]; then
            echo "*** You should download Xcode 8.3.3. Login to:"
            echo " https://developer.apple.com/downloads/"
            echo "then download from:"
            echo " https://download.developer.apple.com/Developer_Tools/Xcode_8.3.3/Xcode8.3.3.xip"
            echo "and then run this script from within the same directory as the downloaded file"
            missingdmg=1
        fi
        if [ "$xc9" = 1 ] && [ ! -f Xcode_9.4.1.xip ]; then
            echo "*** You should download Xcode 9.4.1. Login to:"
            echo " https://developer.apple.com/downloads/"
            echo "then download from:"
            echo " https://download.developer.apple.com/Developer_Tools/Xcode_9.4.1/Xcode_9.4.1.xip"
            echo "and then run this script from within the same directory as the downloaded file"
            missingdmg=1
        fi
        if [ "$missingdmg" = 1 ]; then
            echo "*** at least one Xcode distribution is missing, cannot build packages - exiting now"
            exit
        fi
        if [ "$xc8" = 1 ]; then
            if [ -e Xcode.app ]; then
                echo "*** A stray Xcode.app exists in the XcodeLegacy.sh folder. Remove it then try again."
                exit
            fi
        fi

        MNTDIR="$(mktemp -d mount.XXX)"
        ATTACH_OPTS=(-nobrowse -mountroot "$MNTDIR")
        if [ "$xc3" = 1 ]; then
            # you should download Xcode 3.2.6 from:
            # http://connect.apple.com/cgi-bin/WebObjects/MemberSite.woa/wa/getSoftware?bundleID=20792
            hdiutil attach xcode_3.2.6_and_ios_sdk_4.3.dmg "${ATTACH_OPTS[@]}"
            if [ ! -d "$MNTDIR/Xcode and iOS SDK" ]; then
                echo "*** Error while trying to attach disk image xcode_3.2.6_and_ios_sdk_4.3.dmg"
                echo "Aborting"
                exit
            fi
            if [ "$compilers" = 1 ]; then
                rm -rf /tmp/XC3
                pkgutil --expand "$MNTDIR/Xcode and iOS SDK/Packages/DeveloperTools.pkg" /tmp/XC3
                (cd /tmp/XC3 || exit; gzip -dc Payload | cpio -id --quiet Library/Xcode/Plug-ins) #we only need these, see https://github.com/devernay/xcodelegacy/issues/8
                ( (cd /tmp/XC3/Library/Xcode/Plug-ins || exit; tar cf - "GCC 4.0.xcplugin") | gzip -c > XcodePluginGCC40.tar.gz) && echo "*** Created XcodePluginGCC40.tar.gz in directory $(pwd)"
                ( (cd /tmp/XC3/Library/Xcode/Plug-ins || exit; tar cf - "GCC 4.2.xcplugin") | gzip -c > XcodePluginGCC42.tar.gz) && echo "*** Created XcodePluginGCC42.tar.gz in directory $(pwd)"
                #( (cd /tmp/XC3/Library/Xcode/Plug-ins || exit; tar cf - "LLVM GCC 4.2.xcplugin") | gzip -c > XcodePluginLLVMGCC42.tar.gz) && echo "*** Created XcodePluginLLVMGCC42.tar.gz in directory $(pwd)"
                # should be untarred in /Developer/Library/Xcode/PrivatePlugIns/Xcode3Core.ideplugin/Contents/SharedSupport/Developer/Library/Xcode/Plug-ins
                # gzip -dc XcodePluginGCC40.tar.gz | (cd /Developer/Library/Xcode/PrivatePlugIns/Xcode3Core.ideplugin/Contents/SharedSupport/Developer/Library/Xcode/Plug-ins || exit; sudo tar xvf -)

                rm -rf /tmp/XC3
                pkgutil --expand "$MNTDIR/Xcode and iOS SDK/Packages/DeveloperToolsCLI.pkg" /tmp/XC3

                (cd /tmp/XC3 || exit; gzip -dc Payload | cpio -id --quiet usr/bin usr/libexec) #we only need these, see https://github.com/devernay/xcodelegacy/issues/8
                ( (cd /tmp/XC3 || exit; tar cf - usr/libexec/gcc/darwin/ppc usr/libexec/gcc/darwin/ppc64 usr/libexec/gcc/darwin/i386 usr/libexec/gcc/darwin/x86_64) | gzip -c > Xcode3as.tar.gz) && echo "*** Created Xcode3as.tar.gz in directory $(pwd)"
                ( (cd /tmp/XC3 || exit; tar cf - usr/bin/ld) | gzip -c > Xcode3ld.tar.gz) && echo "*** Created Xcode3ld.tar.gz in directory $(pwd)"

                #(cp "$MNTDIR/Xcode and iOS SDK/Packages/gcc4.0.pkg" xcode_3.2.6_gcc4.0.pkg) && echo "*** Created xcode_3.2.6_gcc4.0.pkg in directory $(pwd)"
                rm -rf /tmp/XC3
                pkgutil --expand "$MNTDIR/Xcode and iOS SDK/Packages/gcc4.0.pkg" /tmp/XC3

                (cd /tmp/XC3 || exit; gzip -dc Payload | cpio -id --quiet usr) #we only need these, see https://github.com/devernay/xcodelegacy/issues/8
                ( (cd /tmp/XC3 || exit; tar cf - usr) | gzip -c > Xcode3gcc40.tar.gz) && echo "*** Created Xcode3gcc40.tar.gz in directory $(pwd)"

                #(cp "$MNTDIR/Xcode and iOS SDK/Packages/gcc4.2.pkg" xcode_3.2.6_gcc4.2.pkg) && echo "*** Created xcode_3.2.6_gcc4.2.pkg in directory $(pwd)"
                rm -rf /tmp/XC3
                pkgutil --expand "$MNTDIR/Xcode and iOS SDK/Packages/gcc4.2.pkg" /tmp/XC3

                (cd /tmp/XC3 || exit; gzip -dc Payload | cpio -id --quiet usr) #we only need these, see https://github.com/devernay/xcodelegacy/issues/8
                ( (cd /tmp/XC3 || exit; tar cf - usr) | gzip -c > Xcode3gcc42.tar.gz) && echo "*** Created Xcode3gcc42.tar.gz in directory $(pwd)"

                #(cp "$MNTDIR/Xcode and iOS SDK/Packages/llvm-gcc4.2.pkg" xcode_3.2.6_llvm-gcc4.2.pkg) && echo "*** Created xcode_3.2.6_llvm-gcc4.2.pkg in directory $(pwd)"
                rm -rf /tmp/XC3
                pkgutil --expand "$MNTDIR/Xcode and iOS SDK/Packages/llvm-gcc4.2.pkg" /tmp/XC3

                (cd /tmp/XC3 || exit; gzip -dc Payload | cpio -id --quiet usr) #we only need these, see https://github.com/devernay/xcodelegacy/issues/8
                ( (cd /tmp/XC3 || exit; tar cf - usr) | gzip -c > Xcode3llvmgcc42.tar.gz) && echo "*** Created Xcode3llvmgcc42.tar.gz in directory $(pwd)"
            fi
            
            rm -rf /tmp/XC3

            if [ "$osx104" = 1 ] || [ "$osx105" = 1 ]; then
                # use the latest version of the hashtable include, as recommended by:
                # http://wiki.inkscape.org/wiki/index.php/HashtableFixOSX
                # http://permalink.gmane.org/gmane.comp.graphics.inkscape.devel/32966
                # The version from gcc 4.0.4 fixes these four bugs:
                #
                # GCC Bugzilla Bug 23053
                # Const-correctness issue in TR1 hashtable
                # <http://gcc.gnu.org/bugzilla/show_bug.cgi?id=23053>
                #
                # GCC Bugzilla Bug 23465
                # Assignment fails on TR1 unordered containers
                # <http://gcc.gnu.org/bugzilla/show_bug.cgi?id=23465>
                #
                # GCC Bugzilla Bug 24054
                # std::tr1::unordered_map's erase does not seem to return a value
                # <http://gcc.gnu.org/bugzilla/show_bug.cgi?id=24054>
                #
                # GCC Bugzilla Bug 24064
                # tr1::unordered_map seems to seg-fault when caching hash values
                # <http://gcc.gnu.org/bugzilla/show_bug.cgi?id=24064>

                # see also:
                # http://wayback.archive.org/web/20100810175143/http://mohri-lt.cs.nyu.edu:80/twiki/bin/view/FST/CompilingOnMacOSX
                # (only fixes GCC Bugzilla Bug 23465)

                #curl -A 'Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.6 (KHTML, like Gecko) Chrome/20.0.1090.0 Safari/536.6' 'https://gcc.gnu.org/viewcvs/gcc/branches/gcc-4_0-branch/libstdc%2B%2B-v3/include/tr1/hashtable?revision=95538&view=co' -o hashtable-gcc-4.0.0
                #curl -A 'Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.6 (KHTML, like Gecko) Chrome/20.0.1090.0 Safari/536.6' 'https://gcc.gnu.org/viewcvs/gcc/branches/gcc-4_0-branch/libstdc%2B%2B-v3/include/tr1/hashtable?revision=104939&view=co' -o hashtable-gcc-4.0.4
                if false; then
                    # older version of the patch, for the record (only fixes 23053 and 23465)
                    cat > /tmp/hashtable.patch <<EOF
--- hashtable.orig	2015-09-01 14:43:32.000000000 +0200
+++ hashtable	2010-09-03 22:41:42.000000000 +0200
@@ -860,7 +860,7 @@
   typedef typename Internal::IF<unique_keys, std::pair<iterator, bool>, iterator>::type
           Insert_Return_Type;
 
-  node* find_node (node* p, const key_type& k, typename hashtable::hash_code_t c);
+  node* find_node (node* p, const key_type& k, typename hashtable::hash_code_t c) const;
 
   std::pair<iterator, bool> insert (const value_type&, std::tr1::true_type);
   iterator insert (const value_type&, std::tr1::false_type);
@@ -1042,8 +1042,9 @@
       node* n = ht.m_buckets[i];
       node** tail = m_buckets + i;
       while (n) {
-	*tail = m_allocate_node (n);
-	(*tail).copy_code_from (n);
+	// 	*tail = m_allocate_node (n);
+	// 	(*tail).copy_code_from (n);
+	*tail = m_allocate_node (n->m_v);
 	tail = &((*tail)->m_next);
 	n = n->m_next;
       }
@@ -1216,7 +1217,7 @@
 	  bool c, bool m, bool u>
 typename hashtable<K,V,A,Ex,Eq,H1,H2,H,RP,c,m,u>::node* 
 hashtable<K,V,A,Ex,Eq,H1,H2,H,RP,c,m,u>
-::find_node (node* p, const key_type& k, typename hashtable::hash_code_t code)
+::find_node (node* p, const key_type& k, typename hashtable::hash_code_t code) const
 {
   for ( ; p ; p = p->m_next)
     if (this->compare (k, code, p))
EOF
                fi
            fi
            
            if [ "$osx104" = 1 ]; then
                test -d /tmp/XC3-10.4 && rm -rf /tmp/XC3-10.4
                pkgutil --expand "$MNTDIR/Xcode and iOS SDK/Packages/MacOSX10.4.Universal.pkg" /tmp/XC3-10.4
                (cd /tmp/XC3-10.4 || exit; gzip -dc Payload | cpio -id --quiet SDKs/MacOSX10.4u.sdk)
                SDKROOT=/tmp/XC3-10.4/SDKs/MacOSX10.4u.sdk
                # should we install more than these? (fixed includes?)
                # Add links to libstdc++ so that "g++-4.0 -isysroot /Developer/SDKs/MacOSX10.4u.sdk -Wl,-syslibroot,/Developer/SDKs/MacOSX10.4u.sdk -mmacosx-version-min=10.4" works
                ln -s ../../../i686-apple-darwin10/4.0.1/libstdc++.dylib $SDKROOT/usr/lib/gcc/i686-apple-darwin10/4.0.1/libstdc++.dylib
                # Add links to libstdc++ so that "clang++ -stdlib=libstdc++ -isysroot /Developer/SDKs/MacOSX10.4u.sdk -Wl,-syslibroot,/Developer/SDKs/MacOSX10.4u.sdk -mmacosx-version-min=10.4" works
                ln -s libstdc++.6.dylib $SDKROOT/usr/lib/libstdc++.dylib
                # Fix tr1/hashtable
                # see http://www.openfst.org/twiki/bin/view/FST/CompilingOnMacOSX https://gcc.gnu.org/ml/libstdc++/2005-08/msg00017.html https://gcc.gnu.org/bugzilla/show_bug.cgi?id=23053
                # in SDKs/MacOSX10.4u.sdk/usr/include/c++/4.0.0/tr1/hashtable
                #(cd $SDKROOT/usr/include/c++/4.0.0/tr1 || exit; patch -p0 -d. < /tmp/hashtable.patch)
                mv $SDKROOT/usr/include/c++/4.0.0/tr1/hashtable $SDKROOT/usr/include/c++/4.0.0/tr1/hashtable.orig
                cp hashtable-gcc-4.0.4 $SDKROOT/usr/include/c++/4.0.0/tr1/hashtable

                # Add links for compatibility with GCC 4.2
                ln -s 4.0.1 $SDKROOT/usr/lib/gcc/i686-apple-darwin10/4.2.1
                ln -s 4.0.1 $SDKROOT/usr/lib/gcc/powerpc-apple-darwin10/4.2.1
                ln -s 4.0.1 $SDKROOT/usr/lib/i686-apple-darwin10/4.2.1
                ln -s 4.0.1 $SDKROOT/usr/lib/powerpc-apple-darwin10/4.2.1
                ln -s 4.0.0 $SDKROOT/usr/include/c++/4.2.1

                ( (cd /tmp/XC3-10.4 || exit; tar cf - SDKs/MacOSX10.4u.sdk) | gzip -c > Xcode104SDK.tar.gz) && echo "*** Created Xcode104SDK.tar.gz in directory $(pwd)"
                rm -rf /tmp/XC3-10.4
            fi
            
            if [ "$osx105" = 1 ]; then
                test -d /tmp/XC3-10.5 && rm -rf /tmp/XC3-10.5
                pkgutil --expand "$MNTDIR/Xcode and iOS SDK/Packages/MacOSX10.5.pkg" /tmp/XC3-10.5
                (cd /tmp/XC3-10.5 || exit; gzip -dc Payload | cpio -id --quiet SDKs/MacOSX10.5.sdk)
                SDKROOT=/tmp/XC3-10.5/SDKs/MacOSX10.5.sdk
                # should we install more than these? (fixed includes?)
                # Add links to libstdc++ so that "g++-4.0 -isysroot /Developer/SDKs/MacOSX10.5.sdk -Wl,-syslibroot,/Developer/SDKs/MacOSX10.5.sdk -mmacosx-version-min=10.5" works
                ln -s ../../../i686-apple-darwin10/4.0.1/libstdc++.dylib $SDKROOT/usr/lib/gcc/i686-apple-darwin10/4.0.1/libstdc++.dylib
                ln -s ../../../i686-apple-darwin10/4.2.1/libstdc++.dylib $SDKROOT/usr/lib/gcc/i686-apple-darwin10/4.2.1/libstdc++.dylib
                # Add links to libstdc++ so that "clang++ -stdlib=libstdc++ -isysroot /Developer/SDKs/MacOSX10.5.sdk -Wl,-syslibroot,/Developer/SDKs/MacOSX10.5.sdk -mmacosx-version-min=10.5" works
                ln -s libstdc++.6.dylib $SDKROOT/usr/lib/libstdc++.dylib
                # fix AvailabilityInternal.h (see https://trac.macports.org/wiki/LeopardSDKFixes)
                sed -i.orig -e 's/define __MAC_OS_X_VERSION_MAX_ALLOWED __MAC_10_6/define __MAC_OS_X_VERSION_MAX_ALLOWED 1058/' $SDKROOT/usr/include/AvailabilityInternal.h
                # Fix tr1/hashtable
                # see http://www.openfst.org/twiki/bin/view/FST/CompilingOnMacOSX https://gcc.gnu.org/ml/libstdc++/2005-08/msg00017.html https://gcc.gnu.org/bugzilla/show_bug.cgi?id=23053
                # in SDKs/MacOSX10.5.sdk/usr/include/c++/4.0.0/tr1/hashtable
                # this also affects g++-4.2, since usr/include/c++/4.2.1 links to usr/include/c++/4.0.0
                #(cd $SDKROOT/usr/include/c++/4.0.0/tr1 || exit; patch -p0 -d. < /tmp/hashtable.patch)
                mv $SDKROOT/usr/include/c++/4.0.0/tr1/hashtable $SDKROOT/usr/include/c++/4.0.0/tr1/hashtable.orig
                cp hashtable-gcc-4.0.4 $SDKROOT/usr/include/c++/4.0.0/tr1/hashtable
            fi

            if [ "$osx104" = 1 ] || [ "$osx105" = 1 ]; then
                true
                #rm /tmp/hashtable.patch
            fi

            if [ $osx105 = 1 ] || [ $osx106 = 1 ]; then
                test -d /tmp/XC3 && rm -rf /tmp/XC3
                pkgutil --expand "$MNTDIR/Xcode and iOS SDK/Packages/MacOSX10.6.pkg" /tmp/XC3
                (cd /tmp/XC3 || exit; gzip -dc Payload | cpio -id --quiet SDKs/MacOSX10.6.sdk)
                SDKROOT=/tmp/XC3/SDKs/MacOSX10.6.sdk
                # should we install more than these? (fixed includes?)
                # Add links to libstdc++ so that "clang++ -stdlib=libstdc++ -isysroot /Developer/SDKs/MacOSX10.6.sdk -Wl,-syslibroot,/Developer/SDKs/MacOSX10.6.sdk -mmacosx-version-min=10.6" works
                ln -s libstdc++.6.dylib $SDKROOT/usr/lib/libstdc++.dylib

                # fix buggy hashtable include (see above for explanations)
                cp hashtable-gcc-4.0.4 $SDKROOT/usr/include/c++/4.0.0/tr1/hashtable

                if [ "$osx105" = 1 ]; then
                    # we also need to copy /usr/lib/libgcc_s.10.5.dylib from 10.6 SDK to 10.5SDK, see https://trac.macports.org/wiki/LeopardSDKFixes
                    # This should fix compiling the following:
                    # int main() { __uint128_t a = 100; __uint128_t b = 200; __uint128_t c = a / b; return 0; }
                    # with clang -isysroot /Developer/SDKs/MacOSX10.5.sdk -Wl,-syslibroot,/Developer/SDKs/MacOSX10.5.sdk -mmacosx-version-min=10.5 conftest1.c
                    cp /tmp/XC3-10.5/SDKs/MacOSX10.5.sdk/usr/lib/libgcc_s.10.5.dylib /tmp/XC3-10.5/SDKs/MacOSX10.5.sdk/usr/lib/libgcc_s.10.5.dylib.bak
                    cp $SDKROOT/usr/lib/libgcc_s.10.5.dylib /tmp/XC3-10.5/SDKs/MacOSX10.5.sdk/usr/lib/libgcc_s.10.5.dylib

                    ( (cd /tmp/XC3-10.5 || exit; tar cf - SDKs/MacOSX10.5.sdk) | gzip -c > Xcode105SDK.tar.gz) && echo "*** Created Xcode105SDK.tar.gz in directory $(pwd)"
                fi
                if [ "$osx106" = 1 ]; then
                    ( (cd /tmp/XC3 || exit; tar cf - SDKs/MacOSX10.6.sdk) | gzip -c > Xcode106SDK.tar.gz) && echo "*** Created Xcode106SDK.tar.gz in directory $(pwd)"
                fi
                rm -rf /tmp/XC3-10.5 /tmp/XC3
            fi
            hdiutil detach "$MNTDIR/Xcode and iOS SDK" -force
        fi

        if [ "$xc4" = 1 ]; then
            hdiutil attach xcode4630916281a.dmg "${ATTACH_OPTS[@]}"
            if [ ! -d "$MNTDIR/Xcode" ]; then
                echo "*** Error while trying to attach disk image xcode4630916281a.dmg"
                echo "Aborting"
                rmdir "$MNTDIR"
                exit
            fi
            if [ "$osx107" = 1 ]; then
                ( (cd "$MNTDIR/Xcode/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Developer" || exit; tar cf - SDKs/MacOSX10.7.sdk) | gzip -c > Xcode107SDK.tar.gz) && echo "*** Created Xcode107SDK.tar.gz in directory $(pwd)"
            fi
            if [ "$compilers" = 1 ]; then
                ( (cd "$MNTDIR/Xcode/Xcode.app/Contents/PlugIns/Xcode3Core.ideplugin/Contents/SharedSupport/Developer/Library/Xcode/Plug-ins" || exit; tar cf - "GCC 4.2.xcplugin") | gzip -c > XcodePluginGCC42-Xcode4.tar.gz) && echo "*** Created XcodePluginGCC42-Xcode4.tar.gz in directory $(pwd)"
                ( (cd "$MNTDIR/Xcode/Xcode.app/Contents/PlugIns/Xcode3Core.ideplugin/Contents/SharedSupport/Developer/Library/Xcode/Plug-ins" || exit; tar cf - "LLVM GCC 4.2.xcplugin") | gzip -c > XcodePluginLLVMGCC42.tar.gz) && echo "*** Created XcodePluginLLVMGCC42.tar.gz in directory $(pwd)"
            fi
            hdiutil detach "$MNTDIR/Xcode" -force
        fi

        if [ "$xc5" = 1 ]; then
            hdiutil attach xcode_5.1.1.dmg "${ATTACH_OPTS[@]}"
            if [ ! -d "$MNTDIR/Xcode" ]; then
                echo "*** Error while trying to attach disk image xcode_5.1.1.dmg"
                echo "Aborting"
                rmdir "$MNTDIR"
                exit
            fi
            if [ "$osx108" = 1 ]; then
                ( (cd "$MNTDIR/Xcode/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Developer" || exit; tar cf - SDKs/MacOSX10.8.sdk) | gzip -c > Xcode108SDK.tar.gz) && echo "*** Created Xcode108SDK.tar.gz in directory $(pwd)"
            fi
            hdiutil detach "$MNTDIR/Xcode" -force
        fi

        if [ "$xc6" = 1 ]; then
            hdiutil attach Xcode_6.4.dmg "${ATTACH_OPTS[@]}"
            if [ ! -d "$MNTDIR/Xcode" ]; then
                echo "*** Error while trying to attach disk image Xcode_6.4.dmg"
                echo "Aborting"
                rmdir "$MNTDIR"
                exit
            fi
            if [ "$osx109" = 1 ]; then
                ( (cd "$MNTDIR/Xcode/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Developer" || exit; tar cf - SDKs/MacOSX10.9.sdk) | gzip -c > Xcode109SDK.tar.gz) && echo "*** Created Xcode109SDK.tar.gz in directory $(pwd)"
            fi
            if [ "$osx1010" = 1 ]; then
                ( (cd "$MNTDIR/Xcode/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Developer" || exit; tar cf - SDKs/MacOSX10.10.sdk) | gzip -c > Xcode1010SDK.tar.gz) && echo "*** Created Xcode1010SDK.tar.gz in directory $(pwd)"
            fi
            hdiutil detach "$MNTDIR/Xcode" -force
        fi
        if [ "$xc7" = 1 ]; then
            hdiutil attach Xcode_7.3.1.dmg "${ATTACH_OPTS[@]}"
            if [ ! -d "$MNTDIR/Xcode" ]; then
                echo "*** Error while trying to attach disk image Xcode_7.3.1.dmg"
                echo "Aborting"
                rmdir "$MNTDIR"
                exit
            fi
            if [ "$osx1011" = 1 ]; then
                ( (cd "$MNTDIR/Xcode/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Developer" || exit; tar cf - SDKs/MacOSX10.11.sdk) | gzip -c > Xcode1011SDK.tar.gz) && echo "*** Created Xcode1011SDK.tar.gz in directory $(pwd)"
            fi
            hdiutil detach "$MNTDIR/Xcode" -force
        fi
        if [ "$xc8" = 1 ]; then
            if [ "$osx1012" = 1 ]; then
                echo "Extracting Mac OS X 10.12 SDK from Xcode 8.3.3. Be patient - this will take some time"
                open Xcode8.3.3.xip
                while [ ! -d Xcode.app ]; do
                    sleep 5
                done
                sleep 5
                ( (cd "Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Developer" || exit; rm SDKs/MacOSX10.12.sdk; mv SDKs/MacOSX.sdk SDKs/MacOSX10.12.sdk; tar cf - SDKs/MacOSX10.12.sdk) | gzip -c > Xcode1012SDK.tar.gz) && echo "*** Created Xcode1012SDK.tar.gz in directory $(pwd)"
                rm -rf Xcode.app
            fi
        fi
        if [ "$xc9" = 1 ]; then
            if [ "$osx1013" = 1 ]; then
                echo "Extracting Mac OS X 10.13 SDK from Xcode 9.4.1. Be patient - this will take some time"
                open Xcode_9.4.1.xip
                while [ ! -d Xcode.app ]; do
                    sleep 5
                done
                sleep 5
                ( (cd "Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Developer" || exit; rm SDKs/MacOSX10.13.sdk; mv SDKs/MacOSX.sdk SDKs/MacOSX10.13.sdk; tar cf - SDKs/MacOSX10.13.sdk) | gzip -c > Xcode1013SDK.tar.gz) && echo "*** Created Xcode1013SDK.tar.gz in directory $(pwd)"
                rm -rf Xcode.app
            fi
        fi
        rmdir "$MNTDIR"
        ;;

    install|installbeta)
        #######################
        # PHASE 2: INSTALLING
        #
        if [ $EUID -ne 0 ]; then
            echo "*** Error: The install phase requires administrative rights. Please run it as:"
            echo " $ sudo $0 install"
            exit 1
        fi
        if [ ! -d "$PLUGINDIR" ]; then
            echo "*** Error: could not find Xcode 4.2 in /Developer/Applications nor Xcode >= 4.3 in /Applications, cannot install"
            exit 1
        fi
        if [ "$compilers" = 1 ]; then
            if [ -d "$PLUGINDIR/GCC 4.0.xcplugin" ]; then
                echo "*** Not installing XcodePluginGCC40.tar.gz (found installed in $PLUGINDIR/GCC 4.0.xcplugin, uninstall first to force install)"
            else
                (gzip -dc XcodePluginGCC40.tar.gz | (cd "$PLUGINDIR" || exit; tar xf -)) && touch "$PLUGINDIR/GCC 4.0.xcplugin/legacy" && echo "*** installed XcodePluginGCC40.tar.gz"
                # Add entries expected by later xcodebuilds.
                mv "$PLUGINDIR/GCC 4.0.xcplugin/Contents/Resources/GCC 4.0.xcspec" "$PLUGINDIR/GCC 4.0.xcplugin/Contents/Resources/GCC 4.0.xcspec-original"
                sed '$ i\
\		ExecDescription = \"Compile \$\(InputFile\)\"\;\
\		ProgressDescription = \"Compiling \$\(InputFile\)\"\;\
\		ExecDescriptionForPrecompile = \"Precompile \$\(InputFile\)\"\;\
\		ProgressDescriptionForPrecompile = \"Precompiling \$\(InputFile\)\"\;
'  < "$PLUGINDIR/GCC 4.0.xcplugin/Contents/Resources/GCC 4.0.xcspec-original" > "$PLUGINDIR/GCC 4.0.xcplugin/Contents/Resources/GCC 4.0.xcspec"

                echo "*** modified GCC 4.0.xcspec"
            fi
            if [ -d "$PLUGINDIR/GCC 4.2.xcplugin" ] && [ ! -f "$PLUGINDIR/GCC 4.2.xcplugin/Contents/Resources/GCC Generic.xcspec" ]; then
                echo "*** Not installing XcodePluginGCC42.tar.gz (found installed in $PLUGINDIR/GCC 4.2.xcplugin, uninstall first to force install)"
            else
                if [ -f "$PLUGINDIR/GCC 4.2.xcplugin/Contents/Resources/GCC Generic.xcspec" ]; then
                    mv "$PLUGINDIR/GCC 4.2.xcplugin" "$PLUGINDIR/GCC 4.2.xcplugin-original"
                fi
                (gzip -dc XcodePluginGCC42.tar.gz | (cd "$PLUGINDIR" || exit; tar xf -)) && touch "$PLUGINDIR/GCC 4.2.xcplugin/legacy" && echo "*** installed XcodePluginGCC42.tar.gz"
                # Add entries expected by later xcodebuilds.
                mv "$PLUGINDIR/GCC 4.2.xcplugin/Contents/Resources/GCC 4.2.xcspec" "$PLUGINDIR/GCC 4.2.xcplugin/Contents/Resources/GCC 4.2.xcspec-original"
                sed '$ i\
\		ExecDescription = \"Compile \$\(InputFile\)\"\;\
\		ProgressDescription = \"Compiling \$\(InputFile\)\"\;\
\		ExecDescriptionForPrecompile = \"Precompile \$\(InputFile\)\"\;\
\		ProgressDescriptionForPrecompile = \"Precompiling \$\(InputFile\)\"\;
'  < "$PLUGINDIR/GCC 4.2.xcplugin/Contents/Resources/GCC 4.2.xcspec-original" > "$PLUGINDIR/GCC 4.2.xcplugin/Contents/Resources/GCC 4.2.xcspec"
                echo "*** modified GCC 4.2.xcspec"
            fi
            if [ -d "$PLUGINDIR/LLVM GCC 4.2.xcplugin" ]; then
                echo "*** Not installing XcodePluginLLVMGCC42.tar.gz (found installed in $PLUGINDIR/LLVM GCC 4.2.xcplugin, uninstall first to force install)"
            else
                (gzip -dc XcodePluginLLVMGCC42.tar.gz | (cd "$PLUGINDIR" || exit; tar xf -)) && touch "$PLUGINDIR/LLVM GCC 4.2.xcplugin/legacy" && echo "*** installed XcodePluginLLVMGCC42.tar.gz"
            fi

            if [ -f "$GCCDIR/usr/libexec/gcc/darwin/ppc/as" ]; then
                echo "*** Not installing Xcode3as.tar.gz (found installed in $GCCDIR/usr/libexec/gcc/darwin/ppc/as, uninstall first to force install)"
            else
                (gzip -dc Xcode3as.tar.gz | (cd "$GCCDIR" || exit; tar xf -))
                mkdir -p "$GCCINSTALLDIR/usr/bin"
                mkdir -p "$GCCINSTALLDIR/usr/libexec/as/ppc"
                mkdir -p "$GCCINSTALLDIR/usr/libexec/as/ppc64"
                mkdir -p "$GCCINSTALLDIR/usr/libexec/as/i386"
                mkdir -p "$GCCINSTALLDIR/usr/libexec/as/x86_64"
                ln -sf "$GCCDIR/usr/libexec/gcc/darwin/ppc/as" "$GCCINSTALLDIR/usr/libexec/as/ppc/as"
                ln -sf "$GCCDIR/usr/libexec/gcc/darwin/ppc64/as" "$GCCINSTALLDIR/usr/libexec/as/ppc64/as"
                # Xcodes >= 4 already include an acceptable GNU legacy assembler
                # (v1.38) for i386 and x86_64 in $GCCINSTALLDIR/usr/libexec/as.
                # When they no longer do, enable these links (conditionally,
                # of course).
                #ln -sf "$GCCDIR/usr/libexec/gcc/darwin/i386/as" "$GCCINSTALLDIR/usr/libexec/as/i386/as"
                #ln -sf "$GCCDIR/usr/libexec/gcc/darwin/x86_64/as" "$GCCINSTALLDIR/usr/libexec/as/x86_64/as"

                # Replace Xcode's modern toolchain assembler with a script
                # that auto-selects the proper legacy assembler based on the
                # command line's -arch parameter. Using a legacy assembler fixes
                # "ld: too many personality routines for compact unwind" errors
                # and "section '__textcoal_nt' is deprecated" warnings emitted
                # by Xcode 7+ assemblers.
                # First, though, don't overwrite the original assembler if
                # XcodeLegacy is installed twice.
                if [ -f "$GCCINSTALLDIR/usr/bin/as" ] && [ ! -f "$GCCINSTALLDIR/usr/bin/as-original" ]; then
                    mv "$GCCINSTALLDIR/usr/bin/as" "$GCCINSTALLDIR/usr/bin/as-original"
                fi
                # NB: While only gcc uses the assembler in our builds (it pipes the
                # output of usr/libexec/gcc/*-apple-darwin10/4.*/ccobj1plus into
                # usr/libexec/gcc/*-apple-darwin10/4.*/as -> usr/bin/as), we can't
                # simply change the link to, say, usr/libexec/gcc/darwin/i386/as
                # because the assembler seems to want the -arch parameter to match
                # its containing folder. Hence, a script (like for ld, below).
                # NB: To keep it simple, the script assumes that anyone invoking
                # the toolchain's usr/bin/as wants to use Xcode 3's assembler.
                # NB: AS_DIR resolves as the directory of the (source) link that
                # invoked the script.

                # Note that we don't look for AS in $AS_DIR/../libexec/as/\$ARCH/as
                # because gprbuild (from GNU Ada) calls as with both -m and -arch
                # flags, and the arch-specific as doesn'b understand -m32 or -m64.
                # We just look for as in a few places, and if it's not there, we
                # look for as-original, starting in the current dir.
                # In any case, we prune -m32 and -m64 from the as args.
                # see https://github.com/devernay/xcodelegacy/issues/33
                cat <<AS_EOF >> "$GCCINSTALLDIR"/usr/bin/as
#!/bin/bash

ARCH=''
ARCH_FOUND=0
AS_ARGS=()
for var in "\$@"
do
        if [ -z "\$ARCH" ] && [ "\$ARCH_FOUND" -eq '1' ]; then
                ARCH="\$var"
                AS_ARGS+=("\$var")
        elif [ "\$var" = '-arch' ]; then
                ARCH_FOUND=1
                AS_ARGS+=("\$var")
        elif [ "\$var" = '-m32' ]; then
                true
        elif [ "\$var" = '-m64' ]; then
                true
        else
                AS_ARGS+=("\$var")
        fi
done

AS_DIR=\`dirname "\$0"\`
AS_FOUND=0
if [ "\$ARCH_FOUND" -eq '1' ]; then
        if [ -x "\$AS_DIR/../../../as/\$ARCH/as" ]; then
                AS="\$AS_DIR/../../../as/\$ARCH/as"
                AS_FOUND=1
        elif [ -x "\$AS_DIR/../../../../../libexec/as/\$ARCH/as" ]; then
                AS="\$AS_DIR/../../../../../libexec/as/\$ARCH/as"
                AS_FOUND=1
        elif [ -x "\$AS_DIR/../../../../../../../usr/libexec/as/\$ARCH/as" ]; then
                AS="\$AS_DIR/../../../../../../../usr/libexec/as/\$ARCH/as"
                AS_FOUND=1
        fi
fi
if [ "\$AS_FOUND" -eq '1' ]; then
        exec \$AS "\${AS_ARGS[@]}"
else
        if [ -x "\$AS_DIR/as-original" ]; then
                ASORIGINAL="\$AS_DIR/as-original"
        elif [ -x "\$AS_DIR/../../../bin/as-original" ]; then
                ASORIGINAL="\$AS_DIR/../../../bin/as-original"
        elif [ -x "\$AS_DIR/../../../../bin/as-original" ]; then
                ASORIGINAL="\$AS_DIR/../../../../bin/as-original"
        elif [ -x "\$AS_DIR/../../../../../bin/as-original" ]; then
                ASORIGINAL="\$AS_DIR/../../../../../bin/as-original"
        else
                echo "Error: cannot find as-original in \$AS_DIR/as-original or \$AS_DIR/../../../bin/as-original or \$AS_DIR/../../../../bin/as-original or \$AS_DIR/../../../../../bin/as-original"
                exit 1
        fi

        exec \$ASORIGINAL "\$@"
fi
AS_EOF
                chmod +x "$GCCINSTALLDIR/usr/bin/as"
                echo "*** installed Xcode3as.tar.gz"
            fi

            if [ -f "$GCCDIR/usr/libexec/gcc/darwin/ppc/ld" ]; then
                echo "*** Not installing Xcode3ld.tar.gz (found installed in $GCCDIR/usr/libexec/gcc/darwin/ppc/ld, uninstall first to force install)"
            elif [ $XCODE42 -eq 1 ]; then
                echo "*** Not installing Xcode3ld.tar.gz (not required for Xcode <= 4.2.1)"
            else
                mkdir -p "$GCCDIR/tmp"
                (gzip -dc Xcode3ld.tar.gz | (cd "$GCCDIR/tmp" || exit; tar xf -))
                cp "$GCCDIR/tmp/usr/bin/ld" "$GCCDIR/usr/libexec/gcc/darwin/ppc/"
                ln "$GCCDIR/usr/libexec/gcc/darwin/ppc/ld" "$GCCDIR/usr/libexec/gcc/darwin/ppc64/ld"
                rm -rf "$GCCDIR/tmp"
                mkdir -p "$GCCINSTALLDIR/usr/libexec/ld/ppc"
                mkdir -p "$GCCINSTALLDIR/usr/libexec/ld/ppc7400"
                mkdir -p "$GCCINSTALLDIR/usr/libexec/ld/ppc970"
                mkdir -p "$GCCINSTALLDIR/usr/libexec/ld/ppc64"
                ln -sf "$GCCDIR/usr/libexec/gcc/darwin/ppc/ld" "$GCCINSTALLDIR/usr/libexec/ld/ppc/ld"
                ln -sf "$GCCDIR/usr/libexec/gcc/darwin/ppc/ld" "$GCCINSTALLDIR/usr/libexec/ld/ppc7400/ld"
                ln -sf "$GCCDIR/usr/libexec/gcc/darwin/ppc/ld" "$GCCINSTALLDIR/usr/libexec/ld/ppc970/ld"
                ln -sf "$GCCDIR/usr/libexec/gcc/darwin/ppc64/ld" "$GCCINSTALLDIR/usr/libexec/ld/ppc64/ld"
                # Xcode 8's ld fails to link i386 and x86_64 for OSX 10.5: https://github.com/devernay/xcodelegacy/issues/30
                # Since this ld is from Xcode 3.2.6 for OSX 10.6, this should be OK if the target OS is < 10.6
                # (which is checked by the stub ld script)
                for arch in i386 x86_64; do
                    mkdir -p "$GCCDIR/usr/libexec/gcc/darwin/$arch"
                    ln "$GCCDIR/usr/libexec/gcc/darwin/ppc/ld" "$GCCDIR/usr/libexec/gcc/darwin/$arch/ld"
                    mkdir -p "$GCCINSTALLDIR/usr/libexec/ld/$arch"
                    ln -sf "$GCCDIR/usr/libexec/gcc/darwin/$arch/ld" "$GCCINSTALLDIR/usr/libexec/ld/$arch/ld"
                done
                # prevent overwriting the original ld if the script is run twice
                if [ ! -f "$GCCINSTALLDIR/usr/bin/ld-original" ]; then
                    mv "$GCCINSTALLDIR/usr/bin/ld" "$GCCINSTALLDIR/usr/bin/ld-original"
                fi
                cat <<LD_EOF >> "$GCCINSTALLDIR"/usr/bin/ld
#!/bin/bash

ARCH=''
ARCH_FOUND=0
for var in "\$@"
do
        if [ "\$ARCH_FOUND" -eq '1' ]; then
                ARCH=\$var
                ARCH_FOUND=2
                break
        else
                case "\$var" in
                        -mmacosx-version-min=10.[0-6])
                                MACOSX_DEPLOYMENT_TARGET=\$( echo \$var | sed -e s/-mmacosx-version-min=// )
                                ;;
                        -arch)
                                if [ "\$ARCH_FOUND" -ne '0' ]; then
                                        echo "Warning: ld: multiple -arch flags"
                                fi
                                ARCH_FOUND=1
                                ;;
                esac
        fi
done

# use the old (Snow Leopard 10.6) ld only if ppc arch or the target macOS is <= 10.6
USE_OLD_LD=0
case "\$ARCH" in
        ppc*) #ppc ppc7400 ppc970 ppc64
                USE_OLD_LD=1
                ;;
esac

if [ -n \${MACOSX_DEPLOYMENT_TARGET+x} ]; then
        # MACOSX_DEPLOYMENT_TARGET can either be set externally as an env variable,
        # or as an ld option using -mmacosx-version-min=10.x
        case "\${MACOSX_DEPLOYMENT_TARGET}" in
                10.[0-6])
                        USE_OLD_LD=1
                        ;;
        esac
fi

#echo "Running ld for \$ARCH ..."

LD_DIR=\`dirname "\$0"\`
if [ -x "\$LD_DIR/ld-original" ]; then
        LDORIGINAL="\$LD_DIR/ld-original"
elif [ -x "\$LD_DIR/../../../../bin/ld-original" ]; then
        LDORIGINAL="\$LD_DIR/../../../../bin/ld-original"
elif [ -x "\$LD_DIR/../../../../../bin/ld-original" ]; then
        LDORIGINAL="\$LD_DIR/../../../../../bin/ld-original"
else
        echo "Error: cannot find ld-original in \$LD_DIR \$LD_DIR/../../../../bin or \$LD_DIR/../../../../../bin"
        exit 1
fi
LD_RESULT=255
if [ "\$USE_OLD_LD" -eq '1' ]; then
        ARGS=()
        # strip the -dependency_info xxx, -object_path_lto xxx, -no_deduplicate, -export_dynamic flags
        DEPINFO_FOUND=0
        OBJECT_PATH_LTO_FOUND=0
        for var in "\$@"; do
                if [ "\$DEPINFO_FOUND" -eq '1' ]; then
                        DEPINFO_FOUND=0
                        continue
                elif [ "\$OBJECT_PATH_LTO_FOUND" -eq '1' ]; then
                        OBJECT_PATH_LTO_FOUND=0
                        continue
                elif [ "\$var" = '-dependency_info' ]; then
                        DEPINFO_FOUND=1
                        continue
                elif [ "\$var" = '-object_path_lto' ]; then
                        OBJECT_PATH_LTO_FOUND=1
                        continue
                elif [ "\$var" = '-no_deduplicate' ]; then
                        continue
                elif [ "\$var" = '-export_dynamic' ]; then
                        continue
                fi

                ARGS+=("\$var")
        done
        # the old ld is put in the ppc dir so as not to disturb more recent archs (i386, x86_64)
        # works with ppc ppc7400 ppc970 ppc64 i386 x86_64
        LDARCHDIR=ppc
        if [ -x "\$LD_DIR/../libexec/ld/\$LDARCHDIR/ld" ]; then
                LD="\$LD_DIR/../libexec/ld/\$LDARCHDIR/ld"
        elif [ -x "\$LD_DIR/../../../libexec/ld/\$LDARCHDIR/ld" ]; then
                LD="\$LD_DIR/../../../libexec/ld/\$LDARCHDIR/ld"
        elif [ -x "\$LD_DIR/../../../../libexec/ld/\$LDARCHDIR/ld" ]; then
                LD="\$LD_DIR/../../../../libexec/ld/\$LDARCHDIR/ld"
        elif [ -x "\$LD_DIR/../../../../../libexec/ld/\$LDARCHDIR/ld" ]; then
                LD="\$LD_DIR/../../../../../libexec/ld/\$LDARCHDIR/ld"
        else
                echo "Error: cannot find ld for \$ARCH in \$LD_DIR/../libexec/ld/\$LDARCHDIR \$LD_DIR/../../../libexec/ld/\$LDARCHDIR \$LD_DIR/../../../../libexec/ld/\$LDARCHDIR or \$LD_DIR/../../../../../libexec/ld/\$LDARCHDIR"
                exit 1
        fi
        
        \`\$LD "\${ARGS[@]}"\`
        LD_RESULT=\$?
else
        \`\$LDORIGINAL "\$@"\`
        LD_RESULT=\$?
fi

exit \$LD_RESULT
LD_EOF
                chmod +x "$GCCINSTALLDIR/usr/bin/ld"
                echo "*** installed Xcode3ld.tar.gz"
            fi

            if [ -f "$PLATFORMDIR/Developer/Library/Xcode/Specifications/MacOSX Architectures.xcspec-original" ]; then
                echo "*** Not modifying MacOSX Architectures.xcspec (found original at $PLATFORMDIR/Developer/Library/Xcode/Specifications/MacOSX Architectures.xcspec-original, uninstall first to force install)"
            else
                mv "$PLATFORMDIR/Developer/Library/Xcode/Specifications/MacOSX Architectures.xcspec" "$PLATFORMDIR/Developer/Library/Xcode/Specifications/MacOSX Architectures.xcspec-original"
                { awk 'NR>1{print l}{l=$0}' "$PLATFORMDIR/Developer/Library/Xcode/Specifications/MacOSX Architectures.xcspec-original"; cat - <<SPEC_EOF; } > "$PLATFORMDIR/Developer/Library/Xcode/Specifications/MacOSX Architectures.xcspec"
        {
                Type = Architecture;
                Identifier = ppc;
                Name = "Minimal (32-bit PowerPC only)";
                Description = "32-bit PowerPC";
                "PerArchBuildSettingName" = PowerPC;
                ByteOrder = big;
                ListInEnum = YES;
                SortNumber = 201;
        },
        {
                Type = Architecture;
                Identifier = ppc7400;
                Name = "PowerPC G4";
                Description = "32-bit PowerPC for G4 processor";
                ByteOrder = big;
                ListInEnum = YES;
                SortNumber = 202;
        },
        {
                Type = Architecture;
                Identifier = ppc970;
                Name = "PowerPC G5 32-bit";
                Description = "32-bit PowerPC for G5 processor";
                ByteOrder = big;
                ListInEnum = YES;
                SortNumber = 203;
        },
        {
                Type = Architecture;
                Identifier = ppc64;
                Name = "PowerPC 64-bit";
                Description = "64-bit PowerPC";
                "PerArchBuildSettingName" = "PowerPC 64-bit";
                ByteOrder = big;
                ListInEnum = YES;
                SortNumber = 204;
        },
)
SPEC_EOF
                echo "*** modified MacOSX Architectures.xcspec"
            fi
        fi
        
        if [ "$osx104" = 1 ]; then
            if [ -d "$SDKDIR/SDKs/MacOSX10.4u.sdk" ]; then
                echo "*** Not installing Xcode104SDK.tar.gz (found installed in $SDKDIR/SDKs/MacOSX10.4u.sdk, uninstall first to force install)"
            else
                (gzip -dc Xcode104SDK.tar.gz | (cd "$SDKDIR" || exit; tar xf -)) && echo "*** installed Xcode104SDK.tar.gz"
                touch "$SDKDIR/SDKs/MacOSX10.4u.sdk/legacy"
            fi
        fi

        if [ "$osx105" = 1 ]; then
            if [ -d "$SDKDIR/SDKs/MacOSX10.5.sdk" ]; then
                echo "*** Not installing Xcode105SDK.tar.gz (found installed in $SDKDIR/SDKs/MacOSX10.5.sdk, uninstall first to force install)"
            else
                (gzip -dc Xcode105SDK.tar.gz | (cd "$SDKDIR" || exit; tar xf -)) && echo "*** installed Xcode105SDK.tar.gz"
                touch "$SDKDIR/SDKs/MacOSX10.5.sdk/legacy"
            fi
        fi
        
        if [ "$osx106" = 1 ]; then
            if [ -d "$SDKDIR/SDKs/MacOSX10.6.sdk" ]; then
                echo "*** Not installing Xcode106SDK.tar.gz (found installed in $SDKDIR/SDKs/MacOSX10.6.sdk, uninstall first to force install)"
            else
                (gzip -dc Xcode106SDK.tar.gz | (cd "$SDKDIR" || exit; tar xf -)) && echo "*** installed Xcode106SDK.tar.gz"
                touch "$SDKDIR/SDKs/MacOSX10.6.sdk/legacy"
            fi
        fi

        if [ "$osx107" = 1 ]; then
            installSDK 10.7
        fi

        if [ "$osx108" = 1 ]; then
            installSDK 10.8
        fi

        if [ "$osx109" = 1 ]; then
            installSDK 10.9
        fi

        if [ "$osx1010" = 1 ]; then
            installSDK 10.10
        fi

        if [ "$osx1011" = 1 ]; then
            installSDK 10.11
        fi

        if [ "$osx1012" = 1 ]; then
            installSDK 10.12
        fi

        if [ "$osx1013" = 1 ]; then
            installSDK 10.13
        fi

        if [ "$compilers" = 1 ]; then
            if [ -f /usr/bin/gcc-4.0 ]; then
                #echo "*** Not installing xcode_3.2.6_gcc4.0.pkg (found installed in /usr/bin/gcc-4.0, uninstall first to force install)"
                echo "*** Not installing Xcode3gcc40.tar.gz (found installed in /usr/bin/gcc-4.0, uninstall first to force install)"
            elif [ -f "$GCCINSTALLDIR/usr/bin/gcc-4.0" ]; then
                echo "*** Not installing Xcode3gcc40.tar.gz (found installed in $GCCINSTALLDIR/usr/bin/gcc-4.0, uninstall first to force install)"
            else
                echo "*** Installing GCC 4.0"
                #installer -pkg xcode_3.2.6_gcc4.0.pkg -target /
                (gzip -dc Xcode3gcc40.tar.gz | (cd "$GCCINSTALLDIR" || exit; tar xf -)) && echo "*** installed Xcode3gcc40.tar.gz"
            fi
            if [ -f /usr/bin/gcc-4.2 ]; then
                #echo "*** Not installing xcode_3.2.6_gcc4.2.pkg (found installed in /usr/bin/gcc-4.2, uninstall first to force install)"
                echo "*** Not installing Xcode3gcc42.tar.gz (found installed in /usr/bin/gcc-4.2, uninstall first to force install)"
            elif [ -f "$GCCINSTALLDIR/usr/bin/gcc-4.2" ]; then
                echo "*** Not installing Xcode3gcc42.tar.gz (found installed in $GCCINSTALLDIR/usr/bin/gcc-4.2, uninstall first to force install)"
            else
                echo "*** Installing GCC 4.2"
                #installer -pkg xcode_3.2.6_gcc4.2.pkg -target /
                (gzip -dc Xcode3gcc42.tar.gz | (cd "$GCCINSTALLDIR" || exit; tar xf -)) && echo "*** installed Xcode3gcc42.tar.gz"
            fi
            if [ -f "$GCCINSTALLDIR/usr/bin/llvm-gcc-4.2" ]; then
                echo "*** Not installing Xcode3llvmgcc42.tar.gz (found installed in $GCCINSTALLDIR/usr/bin/llvm-gcc-4.2, uninstall first to force install)"
            else
                echo "*** Installing LLVM GCC 4.2"
                #installer -pkg xcode_3.2.6_llvm-gcc4.2.pkg -target /
                (gzip -dc Xcode3llvmgcc42.tar.gz | (cd "$GCCINSTALLDIR" || exit; tar xf -)) && echo "*** installed Xcode3llvmgcc42.tar.gz"
                if [ -f "$GCCDIR/usr/llvm-gcc-4.2/bin/llvm-gcc-4.2" ]; then
                    for i in g++ gcc; do
                        ln -sf "$GCCINSTALLDIR"/usr/bin/powerpc-apple-darwin10-llvm-${i}-4.2 "$GCCDIR"/usr/bin/powerpc-apple-darwin"$RELEASENUM"-llvm-${i}-4.2
                        ln -sf "$GCCINSTALLDIR"/usr/llvm-gcc-4.2/bin/powerpc-apple-darwin10-llvm-${i}-4.2 "$GCCDIR"/usr/llvm-gcc-4.2/bin/powerpc-apple-darwin"$RELEASENUM"-llvm-${i}-4.2
                        ln -sf "$GCCINSTALLDIR"/usr/llvm-gcc-4.2/share/man/man1/powerpc-apple-darwin10-llvm-${i}.1.gz "$GCCDIR"/usr/llvm-gcc-4.2/share/man/man1/powerpc-apple-darwin"$RELEASENUM"-llvm-${i}.1.gz
                    done
                    ln -sf "$GCCINSTALLDIR"/usr/llvm-gcc-4.2/lib/gcc/powerpc-apple-darwin10 "$GCCDIR"/usr/llvm-gcc-4.2/lib/gcc/powerpc-apple-darwin10
                    ln -sf "$GCCINSTALLDIR"/usr/llvm-gcc-4.2/libexec/gcc/powerpc-apple-darwin10 "$GCCDIR"/usr/llvm-gcc-4.2/libexec/gcc/powerpc-apple-darwin10
                fi
            fi
            
            echo "*** Creating symbolic links to compliers in $GCCDIR and $GCCLINKDIR:"
            if [ ! -d "$GCCDIR"/usr/bin ]; then
                mkdir -p "$GCCDIR"/usr/bin
            fi
            if [ ! -d "$GCCLINKDIR"/bin ]; then
                mkdir -p "$GCCLINKDIR"/bin
            fi
            for v in 4.0 4.2 4.0.1 4.2.1; do
                for i in c++ cpp g++ gcc gcov llvm-cpp llvm-g++ llvm-gcc; do
                    for p in i686-apple-darwin10- powerpc-apple-darwin10- ""; do
                        if [ -f "$GCCINSTALLDIR"/usr/bin/${p}${i}-${v} ]; then
                            echo "$GCCINSTALLDIR"/usr/bin/${p}${i}-${v} exists
                            if [ ! -f "$GCCLINKDIR"/bin/${p}${i}-${v} ]; then
                                echo "* creating link $GCCLINKDIR/bin/${p}${i}-${v}"
                                ln -sf "$GCCINSTALLDIR"/usr/bin/${p}${i}-${v} "$GCCLINKDIR"/bin/${p}${i}-${v}
                            fi
                            if [ ! -f "$GCCDIR"/usr/bin/${p}${i}-${v} ]; then
                                echo "* creating link $GCCDIR/usr/bin/${p}${i}-${v}"
                                ln -sf "$GCCINSTALLDIR"/usr/bin/${p}${i}-${v} "$GCCDIR"/usr/bin/${p}${i}-${v}
                            fi
                        fi
                    done
                done
            done
            # fix /usr/bin/gcc, see https://github.com/devernay/xcodelegacy/issues/19
            if [ -x /usr/bin/gcc ] && [ ! -x "$GCCINSTALLDIR/usr/bin/gcc" ] && [ -x "$GCCINSTALLDIR/usr/bin/clang" ]; then
                # "xcode-select -r" sets /usr/bin/gcc to be the first gcc found in $GCCINSTALLDIR, which happens to be
                # the directory $GCCINSTALLDIR/usr/libexec/gcc, and results in the following error:
                # $ gcc
                # gcc: error: can't exec '/Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/libexec/gcc' (errno=Permission denied)
                # by putting a link to clang (which is the default Xcode behavior), we fix this
                ln -s clang "$GCCINSTALLDIR/usr/bin/gcc"
                # run gcc once so that xcode-select finds the right file for gcc
                gcc 1>/dev/null 2>/dev/null
            fi
        fi

        # Xcode >= 7.3 disables support for older SDKs in /Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Info.plist
        # see https://github.com/devernay/xcodelegacy/issues/23
        if [ -f "$PLATFORMDIR/Info.plist-original" ]; then
            echo "*** Not modifying MacOSX Info.plist (found original at $PLATFORMDIR/Info.plist-original, uninstall first to force install)"
        elif [ -f "$PLATFORMDIR/Info.plist" ]; then
            mv "$PLATFORMDIR/Info.plist" "$PLATFORMDIR/Info.plist-original"
            plutil -remove MinimumSDKVersion -o "$PLATFORMDIR/Info.plist" "$PLATFORMDIR/Info.plist-original"
            echo "*** modified MacOSX Info.plist"
        fi

        if [ ! -L /Developer/SDKs ] && [ $XCODE42 -ne 1 ]; then
            echo "*** Warning: /Developer/SDKs should be a symlink to $SDKDIR/SDKs"
            echo "Check that /Developer exists, and fix /Developer/SDKs with:"
            echo " $ sudo ln -sf '$SDKDIR/SDKs' /Developer/SDKs"
        fi
        ;;

    cleanpackages)
        #######################
        # PHASE 3: CLEANING
        #

        if [ "$compilers" = 1 ]; then
            rm XcodePluginGCC40.tar.gz Xcode3as.tar.gz Xcode3ld.tar.gz xcode_3.2.6_gcc4.0.pkg xcode_3.2.6_gcc4.2.pkg xcode_3.2.6_llvm-gcc4.2.pkg XcodePluginGCC42-Xcode4.tar.gz XcodePluginGCC42.tar.gz XcodePluginLLVMGCC42.tar.gz Xcode3gcc40.tar.gz Xcode3gcc42.tar.gz Xcode3llvmgcc42.tar.gz 2>/dev/null
        fi
        #for i in 10.4u 10.5 10.6 10.7 10.8 10.9 10.10; do
        if [ "$osx104" = 1 ]; then
            rm Xcode104SDK.tar.gz 2>/dev/null
        fi
        if [ "$osx105" = 1 ]; then
            rm Xcode105SDK.tar.gz 2>/dev/null
        fi
        if [ "$osx106" = 1 ]; then
            rm Xcode106SDK.tar.gz 2>/dev/null
        fi
        if [ "$osx107" = 1 ]; then
            rm Xcode107SDK.tar.gz 2>/dev/null
        fi
        if [ "$osx108" = 1 ]; then
            rm Xcode108SDK.tar.gz 2>/dev/null
        fi
        if [ "$osx109" = 1 ]; then
            rm Xcode109SDK.tar.gz 2>/dev/null
        fi
        if [ "$osx1010" = 1 ]; then
            rm Xcode1010SDK.tar.gz 2>/dev/null
        fi
        if [ "$osx1011" = 1 ]; then
            rm Xcode1011SDK.tar.gz 2>/dev/null
        fi
        if [ "$osx1012" = 1 ]; then
            rm Xcode1012SDK.tar.gz 2>/dev/null
        fi
        if [ "$osx1012" = 1 ]; then
            rm Xcode1013SDK.tar.gz 2>/dev/null
        fi

        ;;

    uninstall|uninstallbeta)
        #######################
        # PHASE 4: UNINSTALLING
        #
        if [ $EUID -ne 0 ]; then
            echo "*** Error: The uninstall phase requires administrative rights. Please run it as:"
            echo " $ sudo $0 uninstall"
            exit 1
        fi

        if [ "$compilers" = 1 ]; then
            if [ -f "$PLUGINDIR/GCC 4.0.xcplugin/legacy" ]; then
                rm -rf "$PLUGINDIR/GCC 4.0.xcplugin"
            fi
            if [ -f "$PLUGINDIR/GCC 4.2.xcplugin/legacy" ]; then
                rm -rf "$PLUGINDIR/GCC 4.2.xcplugin"
            fi
            if [ -d "$PLUGINDIR/GCC 4.2.xcplugin-original" ]; then
                mv "$PLUGINDIR/GCC 4.2.xcplugin-original" "$PLUGINDIR/GCC 4.2.xcplugin"
            fi
            if [ -f "$PLUGINDIR/LLVM GCC 4.2.xcplugin/legacy" ]; then
                rm -rf "$PLUGINDIR/LLVM GCC 4.2.xcplugin"
            fi
            for f in     "$GCCDIR/usr/libexec/gcc/darwin/ppc" \
                         "$GCCDIR/usr/libexec/gcc/darwin/ppc64" \
                         "$GCCDIR/usr/libexec/gcc/darwin/i386" \
                         "$GCCDIR/usr/libexec/gcc/darwin/x86_64" \
                         "$GCCINSTALLDIR/usr/libexec/as/ppc" \
                         "$GCCINSTALLDIR/usr/libexec/as/ppc64" \
                         "$GCCINSTALLDIR/usr/libexec/ld/ppc" \
                         "$GCCINSTALLDIR/usr/libexec/ld/ppc7400" \
                         "$GCCINSTALLDIR/usr/libexec/ld/ppc970" \
                         "$GCCINSTALLDIR/usr/libexec/ld/ppc64" \
                         "$GCCINSTALLDIR/usr/libexec/ld/i386" \
                         "$GCCINSTALLDIR/usr/libexec/ld/x86_64"; do
                    if [ -e "$f" ]; then
                            rm -rf "$f"
                    fi
            done
            if [ -f "$GCCINSTALLDIR/usr/bin/as-original" ]; then
                rm "$GCCINSTALLDIR/usr/bin/as"
                mv -f "$GCCINSTALLDIR/usr/bin/as-original" "$GCCINSTALLDIR/usr/bin/as"
            fi
            if [ -f "$GCCINSTALLDIR/usr/bin/ld-original" ]; then
                rm "$GCCINSTALLDIR/usr/bin/ld"
                mv -f "$GCCINSTALLDIR/usr/bin/ld-original" "$GCCINSTALLDIR/usr/bin/ld"
            fi
            # preserve original LLVM-GCC on Xcode 4 and earlier
            if [ ! -d "$GCCDIR/Library/Perl" ] || [ -d "$GCCDIR/Library/Perl/5.10" ]; then
                mv "$GCCDIR"/usr/bin/{gcov,i686-apple-darwin"$RELEASENUM"-llvm-g{++,cc},llvm-{cpp,g++,gcc}}-4.2 "$GCCDIR"
                (cd "$GCCDIR" || exit; rm -rf $GCCFILES )
                mv "$GCCDIR"/*-4.2 "$GCCDIR"/usr/bin
                (cd "$GCCDIR/usr/llvm-gcc-4.2" || exit; rm -f {bin,lib/gcc,libexec/gcc,share/man/man1}/powerpc*)
            else
                [ -f "$GCCDIR/usr/bin/gcov-4.2" ] && [ ! -L "$GCCDIR/usr/bin/gcov-4.2" ] && mv "$GCCDIR/usr/bin/gcov-4.2" "$GCCDIR"
                (cd "$GCCDIR" || exit; rm -rf $GCCFILES $LLVMGCCFILES)
                [ -f "$GCCDIR/gcov-4.2" ] && mv "$GCCDIR/gcov-4.2" "$GCCDIR/usr/bin"
            fi
            (cd "$GCCINSTALLDIR" || exit; rm -rf $GCCFILES $LLVMGCCFILES)
            rmdir "$GCCINSTALLDIR/usr/include/gcc/darwin" "$GCCINSTALLDIR/usr/include/gcc" || :
            rmdir "$GCCINSTALLDIR/usr/lib/"{i686-apple-darwin10,powerpc-apple-darwin10}"/4.2.1" "$GCCINSTALLDIR/usr/lib/"{gcc/,}{i686-apple-darwin10,powerpc-apple-darwin10} "$GCCINSTALLDIR/usr/lib/gcc" || :
            rmdir "$GCCINSTALLDIR/usr/libexec/gcc/"{i686-apple-darwin10,powerpc-apple-darwin10} "$GCCINSTALLDIR/usr/libexec/gcc" "$GCCINSTALLDIR/usr/libexec/ld" "$GCCDIR/usr/libexec/gcc/darwin" "$GCCDIR/usr/libexec/gcc" || :
            rmdir "$GCCINSTALLDIR/usr/share/man/man7" || :
            if [ -f "$PLATFORMDIR/Developer/Library/Xcode/Specifications/MacOSX Architectures.xcspec-original" ]; then
                rm "$PLATFORMDIR/Developer/Library/Xcode/Specifications/MacOSX Architectures.xcspec"
                mv -f "$PLATFORMDIR/Developer/Library/Xcode/Specifications/MacOSX Architectures.xcspec-original" "$PLATFORMDIR/Developer/Library/Xcode/Specifications/MacOSX Architectures.xcspec"
            fi
        fi
        #for i in 10.4u 10.5 10.6 10.7 10.8 10.9 10.10; do
        if [ "$osx104" = 1 ]; then
            i=10.4u
            [ -f "$SDKDIR/SDKs/MacOSX${i}.sdk/legacy" ] && rm -rf "$SDKDIR/SDKs/MacOSX${i}.sdk"
        fi
        if [ "$osx105" = 1 ]; then
            i=10.5
            [ -f "$SDKDIR/SDKs/MacOSX${i}.sdk/legacy" ] && rm -rf "$SDKDIR/SDKs/MacOSX${i}.sdk"
        fi
        if [ "$osx106" = 1 ]; then
            i=10.6
            [ -f "$SDKDIR/SDKs/MacOSX${i}.sdk/legacy" ] && rm -rf "$SDKDIR/SDKs/MacOSX${i}.sdk"
        fi
        if [ "$osx107" = 1 ]; then
            i=10.7
            [ -f "$SDKDIR/SDKs/MacOSX${i}.sdk/legacy" ] && rm -rf "$SDKDIR/SDKs/MacOSX${i}.sdk"
        fi
        if [ "$osx108" = 1 ]; then
            i=10.8
            [ -f "$SDKDIR/SDKs/MacOSX${i}.sdk/legacy" ] && rm -rf "$SDKDIR/SDKs/MacOSX${i}.sdk"
        fi
        if [ "$osx109" = 1 ]; then
            i=10.9
            [ -f "$SDKDIR/SDKs/MacOSX${i}.sdk/legacy" ] && rm -rf "$SDKDIR/SDKs/MacOSX${i}.sdk"
        fi
        if [ "$osx1010" = 1 ]; then
            i=10.10
            [ -f "$SDKDIR/SDKs/MacOSX${i}.sdk/legacy" ] && rm -rf "$SDKDIR/SDKs/MacOSX${i}.sdk"
        fi
        if [ "$osx1011" = 1 ]; then
            i=10.11
            [ -f "$SDKDIR/SDKs/MacOSX${i}.sdk/legacy" ] && rm -rf "$SDKDIR/SDKs/MacOSX${i}.sdk"
        fi
        if [ "$osx1012" = 1 ]; then
            i=10.12
            [ -f "$SDKDIR/SDKs/MacOSX${i}.sdk/legacy" ] && rm -rf "$SDKDIR/SDKs/MacOSX${i}.sdk"
        fi
        if [ "$osx1013" = 1 ]; then
            i=10.13
            [ -f "$SDKDIR/SDKs/MacOSX${i}.sdk/legacy" ] && rm -rf "$SDKDIR/SDKs/MacOSX${i}.sdk"
        fi
        
        if [ "$compilers" = 1 ]; then
            if [ "$GCCINSTALLDIR/usr/bin/gcc" -ef "$GCCINSTALLDIR/usr/bin/clang" ]; then
                rm "$GCCINSTALLDIR/usr/bin/gcc"
            fi
            for b in llvm-g++ llvm-gcc; do
                if [ -L $GCCINSTALLDIR/usr/bin/$b ] && [ ! -e $GCCINSTALLDIR/usr/bin/$b ]; then
                    rm $GCCINSTALLDIR/usr/bin/$b
                fi
            done
            for b in c++-4.0 cpp-4.0 c++-4.2 cpp-4.2 gcc-4.0 g++-4.0 gcov-4.0 gcc-4.2 g++-4.2 gcov-4.2 llvm-cpp-4.2 llvm-g++-4.2 llvm-gcc-4.2; do
                if [ -L $GCCLINKDIR/bin/$b ] && [ ! -e $GCCLINKDIR/bin/$b ]; then
                    rm $GCCLINKDIR/bin/$b
                fi
            done
            for b in cpp-4.2.1 gcc-4.0.1 g++-4.0.1 gcc-4.2.1 g++-4.2.1 llvm-g++-4.2 llvm-gcc-4.2; do
                if [ -L $GCCLINKDIR/bin/i686-apple-darwin10-$b ] && [ ! -e $GCCLINKDIR/bin/i686-apple-darwin10-$b ]; then
                    rm $GCCLINKDIR/bin/i686-apple-darwin10-$b
                fi
            done
            for b in cpp-4.2.1 gcc-4.0.1 g++-4.0.1 gcc-4.2.1 g++-4.2.1 llvm-g++-4.2 llvm-gcc-4.2; do
                if [ -L $GCCLINKDIR/bin/powerpc-apple-darwin10-$b ] && [ ! -e $GCCLINKDIR/bin/powerpc-apple-darwin10-$b ]; then
                    rm $GCCLINKDIR/bin/powerpc-apple-darwin10-$b
                fi
            done
        fi
        if [ -f "$PLATFORMDIR/Info.plist-original" ] && [ $(ls -1 "$SDKDIR"/SDKs/MacOSX*.sdk/legacy 2>/dev/null | wc -l) -eq 0 ]; then
            rm "$PLATFORMDIR/Info.plist"
            mv -f "$PLATFORMDIR/Info.plist-original" "$PLATFORMDIR/Info.plist"
        fi

        ;;

esac



# Local variables:
# mode: shell-script
# sh-basic-offset: 4
# sh-indent-comment: t
# indent-tabs-mode: nil
# End:
# ex: ts=4 sw=4 et filetype=sh
