﻿namespace SoundFingerprinting.Tests.Unit
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using SoundFingerprinting.Configuration;
    using SoundFingerprinting.FFT;
    using SoundFingerprinting.Utils;
    using SoundFingerprinting.Wavelets;

    [TestClass]
    public class FingerprintServiceTest : AbstractTest
    {
        private FingerprintService fingerprintService;

        private Mock<IFingerprintDescriptor> fingerprintDescriptor;

        private Mock<ISpectrumService> spectrumService;

        private Mock<IWaveletDecomposition> waveletDecomposition;

        [TestInitialize]
        public void SetUp()
        {
            fingerprintDescriptor = new Mock<IFingerprintDescriptor>(MockBehavior.Strict);
            spectrumService = new Mock<ISpectrumService>(MockBehavior.Strict);
            waveletDecomposition = new Mock<IWaveletDecomposition>(MockBehavior.Strict);
            fingerprintService = new FingerprintService(spectrumService.Object, waveletDecomposition.Object, fingerprintDescriptor.Object);
        }

        [TestCleanup]
        public void TearDown()
        {
            fingerprintDescriptor.VerifyAll();
            spectrumService.VerifyAll();
            waveletDecomposition.VerifyAll();
        }

        [TestMethod]
        public void CreateFingerprintsTest()
        {
            var samples = TestUtilities.GenerateRandomAudioSamples(5512 * 10);
            var configuration = SpectrogramConfig.Default;
            var fingerprintConfig = FingerprintConfiguration.Default;
            var dividedLogSpectrum = new List<SpectralImage>
                {
                    new SpectralImage { Image = new[] { TestUtilities.GenerateRandomFloatArray(2048) } },
                    new SpectralImage { Image = new[] { TestUtilities.GenerateRandomFloatArray(2048) } },
                    new SpectralImage { Image = new[] { TestUtilities.GenerateRandomFloatArray(2048) } }
                };
            spectrumService.Setup(service => service.CreateLogSpectrogram(samples, configuration)).Returns(dividedLogSpectrum);
            waveletDecomposition.Setup(service => service.DecomposeImageInPlace(It.IsAny<float[][]>()));
            fingerprintDescriptor.Setup(descriptor => descriptor.ExtractTopWavelets(It.IsAny<float[][]>(), fingerprintConfig.TopWavelets)).Returns(GenericFingerprint);

            List<bool[]> rawFingerprints = fingerprintService.CreateFingerprints(samples, fingerprintConfig);

            Assert.AreEqual(3, rawFingerprints.Count);
            foreach (bool[] fingerprint in rawFingerprints)
            {
                Assert.AreEqual(GenericFingerprint, fingerprint);
            }
        }

        [TestMethod]
        public void SilenceIsNotFingerprinted()
        {
            var samples = TestUtilities.GenerateRandomAudioSamples(5512 * 10);
            var configuration = FingerprintConfiguration.Default;
            var spectrogramConfig = SpectrogramConfig.Default;
            var dividedLogSpectrum = new List<SpectralImage>
                {
                    new SpectralImage { Image = new[] { TestUtilities.GenerateRandomFloatArray(2048) } } 
                };

            spectrumService.Setup(service => service.CreateLogSpectrogram(samples, spectrogramConfig)).Returns(
                dividedLogSpectrum);

            waveletDecomposition.Setup(
                decomposition => decomposition.DecomposeImageInPlace(It.IsAny<float[][]>()));
            fingerprintDescriptor.Setup(
                descriptor => descriptor.ExtractTopWavelets(It.IsAny<float[][]>(), configuration.TopWavelets)).Returns(
                    new[] { false, false, false, false, false, false, false, false });

            List<bool[]> rawFingerprints = fingerprintService.CreateFingerprints(samples, configuration);

            Assert.IsTrue(rawFingerprints.Count == 0);
        }
    }
}
