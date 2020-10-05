class Castxml020 < Formula
  desc "C-family Abstract Syntax Tree XML Output"
  homepage "https://github.com/CastXML/CastXML"
  url "https://github.com/CastXML/CastXML/archive/v0.2.0.tar.gz"
  sha256 "626c395d0d3c777b5a1582cdfc4d33d142acfb12204ebe251535209126705ec1"
  head "https://github.com/CastXML/castxml.git"

  depends_on "cmake" => :build
  depends_on "llvm@9"

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

