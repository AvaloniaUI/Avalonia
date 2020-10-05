class Castxml020 < Formula
  desc "C-family Abstract Syntax Tree XML Output"
  homepage "https://github.com/CastXML/CastXML"
  url "https://github.com/CastXML/CastXML/archive/v0.2.0.tar.gz"
  sha256 "626c395d0d3c777b5a1582cdfc4d33d142acfb12204ebe251535209126705ec1"
  head "https://github.com/CastXML/castxml.git"

  bottle do
    cellar :any
    sha256 "5ec79b331bd18ac2d619c2acb01c42ccfabac62898f5e83971d9594fea1e91ed" => :catalina
    sha256 "3a87a080247a21ab0f05db2dafb826664bb88426563507bb6a00d9c465d41e62" => :mojave
    sha256 "295056ef0feae25c6f00c1e7e669f7f017bc3242c4cbdb0d6c95b34568b31655" => :high_sierra
    sha256 "aaf5927a5f3dfcdc3c88a936a2aa6964ff8f304c48a0690087e6350ef75b0206" => :sierra
  end

  depends_on "cmake" => :build
  depends_on "llvm"

  def install
    mkdir "build" do
      system "cmake", "..", *std_cmake_args
      system "make", "install"
    end
  end

  test do
    (testpath/"test.cpp").write <<~EOS
      int main() {
        return 0;
      }
    EOS
    system "#{bin}/castxml", "-c", "-x", "c++", "--castxml-cc-gnu", "clang++",
                             "--castxml-gccxml", "-o", "test.xml", "test.cpp"
  end
end

